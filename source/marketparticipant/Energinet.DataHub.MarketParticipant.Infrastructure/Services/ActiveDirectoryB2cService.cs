// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public sealed class ActiveDirectoryB2CService : IActiveDirectoryB2CService
    {
        private const string ActorApplicationRegistrationDisplayNamePrefix = "Actor";
        private readonly GraphServiceClient _graphClient;
        private readonly AzureAdConfig _azureAdConfig;
        private readonly IActiveDirectoryB2BRolesProvider _activeDirectoryB2BRolesProvider;

        public ActiveDirectoryB2CService(
            GraphServiceClient graphClient,
            AzureAdConfig config,
            IActiveDirectoryB2BRolesProvider activeDirectoryB2BRolesProvider)
        {
            _graphClient = graphClient;
            _azureAdConfig = config;
            _activeDirectoryB2BRolesProvider = activeDirectoryB2BRolesProvider;
        }

        public async Task AssignApplicationRegistrationAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));

            var permissions = actor.MarketRoles.Select(m => m.Function).ToList();
            var b2CPermissions = (await MapEicFunctionsToB2CIdsAsync(permissions).ConfigureAwait(false)).ToList();
            var enumeratedPermissions = b2CPermissions.ToList();
            var permissionsToPass = enumeratedPermissions.Select(x => x.ToString()).ToList();
            var app = await EnsureAppAsync(actor, permissionsToPass).ConfigureAwait(false);

            var servicePrincipal = await EnsureServicePrincipalToAppAsync(app).ConfigureAwait(false);

            foreach (var permission in enumeratedPermissions)
            {
                await GrantAddedRoleToServicePrincipalAsync(
                        servicePrincipal.Id!,
                        permission)
                    .ConfigureAwait(false);
            }

            actor.ExternalActorId = new ExternalActorId(Guid.Parse(app.AppId!));
        }

        public async Task DeleteAppRegistrationAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var actorApp = await FindApplicationRegistrationAsync(actor).ConfigureAwait(false);
            if (actorApp is null)
            {
                return;
            }

            // Remove service Principal for actor application registration
            var appServicePrincipal = await GetServicePrincipalAsync(actorApp).ConfigureAwait(false);
            if (appServicePrincipal is not null)
            {
                await _graphClient.ServicePrincipals[appServicePrincipal.Id]
                    .DeleteAsync()
                    .ConfigureAwait(false);
            }

            // Remove actor application
            await _graphClient.Applications[actorApp.Id]
                .DeleteAsync()
                .ConfigureAwait(false);

            actor.ExternalActorId = null;
        }

        private static string GenerateActorDisplayName(Actor actor)
        {
            return $"{ActorApplicationRegistrationDisplayNamePrefix}_{actor.ActorNumber.Value}_{actor.Id}";
        }

        private async Task<ServicePrincipal?> GetServicePrincipalAsync(Microsoft.Graph.Models.Application application)
        {
            var response = await _graphClient
                .ServicePrincipals
                .GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"appId eq '{application.AppId}'";
                })
                .ConfigureAwait(false);

            var servicePrincipals = await response!
                .IteratePagesAsync<ServicePrincipal, ServicePrincipalCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            return servicePrincipals.FirstOrDefault();
        }

        private async Task<Microsoft.Graph.Models.Application> EnsureAppAsync(Actor actor, IEnumerable<string> permissions)
        {
            var app = await FindApplicationRegistrationAsync(actor).ConfigureAwait(false);
            if (app is not null)
                return app;

            app = await CreateAppInB2CAsync(actor, permissions).ConfigureAwait(false);
            return app;
        }

        private async Task<ServicePrincipal> EnsureServicePrincipalToAppAsync(Microsoft.Graph.Models.Application app)
        {
            var response = await _graphClient.ServicePrincipals
                .GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"appId eq '{app.AppId}'";
                })
                .ConfigureAwait(false);

            var servicePrincipals = await response!
                .IteratePagesAsync<ServicePrincipal, ServicePrincipalCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            var servicePrincipal = servicePrincipals.FirstOrDefault();

            if (servicePrincipal is not null)
            {
                return servicePrincipal;
            }

            return (await _graphClient.ServicePrincipals
                .PostAsync(new ServicePrincipal
                {
                    AppId = app.AppId,
                })
                .ConfigureAwait(false))!;
        }

        private async Task<Microsoft.Graph.Models.Application?> FindApplicationRegistrationAsync(Actor actor)
        {
            var applicationCollectionResponse = await _graphClient
                .Applications
                .GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"displayName eq '{GenerateActorDisplayName(actor)}'";
                })
                .ConfigureAwait(false);

            var applications = await applicationCollectionResponse!
                .IteratePagesAsync<Microsoft.Graph.Models.Application, ApplicationCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            return applications.FirstOrDefault();
        }

        private async Task<IEnumerable<Guid>> MapEicFunctionsToB2CIdsAsync(IEnumerable<EicFunction> eicFunctions)
        {
            var mappedEicFunction = await _activeDirectoryB2BRolesProvider.GetB2BRolesAsync().ConfigureAwait(false);
            var b2CIds = new List<Guid>();

            foreach (var eicFunction in eicFunctions)
            {
                if (mappedEicFunction.EicRolesMapped.TryGetValue(eicFunction, out var value))
                    b2CIds.Add(value);
            }

            return b2CIds;
        }

        private async ValueTask GrantAddedRoleToServicePrincipalAsync(
            string consumerServicePrincipalObjectId,
            Guid roleId)
        {
            var appRole = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(consumerServicePrincipalObjectId),
                ResourceId = Guid.Parse(_azureAdConfig.BackendAppServicePrincipalObjectId),
                AppRoleId = roleId,
            };

            var role = await _graphClient.ServicePrincipals[consumerServicePrincipalObjectId]
                .AppRoleAssignedTo
                .PostAsync(appRole).ConfigureAwait(false);

            if (role is null)
            {
                throw new InvalidOperationException($"The object: '{nameof(role)}' is null.");
            }
        }

        private async Task<Microsoft.Graph.Models.Application> CreateAppInB2CAsync(
            Actor actor,
            IEnumerable<string> permissions)
        {
            var resourceAccesses = permissions.Select(permission =>
                new ResourceAccess
                {
                    Id = Guid.Parse(permission),
                    Type = "Role",
                }).ToList();

            return (await _graphClient.Applications
                .PostAsync(new Microsoft.Graph.Models.Application
                {
                    DisplayName = GenerateActorDisplayName(actor),
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2,
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    RequiredResourceAccess = new List<RequiredResourceAccess>
                    {
                        new()
                        {
                            ResourceAppId = _azureAdConfig.BackendAppId,
                            ResourceAccess = resourceAccesses,
                        },
                    },
                    SignInAudience = SignInAudience.AzureADMultipleOrgs.ToString(),
                }).ConfigureAwait(false))!;
        }
    }
}
