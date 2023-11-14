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
using Energinet.DataHub.MarketParticipant.Domain.Model.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Applications.Item.RemovePassword;
using Microsoft.Graph.Models;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public sealed class ActiveDirectoryB2CService : IActiveDirectoryB2CService
    {
        private const string SecretDisplayName = "B2C Login - Secret";
        private readonly GraphServiceClient _graphClient;
        private readonly AzureAdConfig _azureAdConfig;
        private readonly IActiveDirectoryB2BRolesProvider _activeDirectoryB2BRolesProvider;
        private readonly ILogger<ActiveDirectoryB2CService> _logger;

        public ActiveDirectoryB2CService(
            GraphServiceClient graphClient,
            AzureAdConfig config,
            IActiveDirectoryB2BRolesProvider activeDirectoryB2BRolesProvider,
            ILogger<ActiveDirectoryB2CService> logger)
        {
            _graphClient = graphClient;
            _azureAdConfig = config;
            _activeDirectoryB2BRolesProvider = activeDirectoryB2BRolesProvider;
            _logger = logger;
        }

        public async Task<CreateAppRegistrationResponse> CreateAppRegistrationAsync(
            ActorNumber actorNumber,
            IReadOnlyCollection<EicFunction> permissions)
        {
            ArgumentNullException.ThrowIfNull(actorNumber, nameof(actorNumber));
            ArgumentNullException.ThrowIfNull(permissions, nameof(permissions));

            var b2CPermissions = (await MapEicFunctionsToB2CIdsAsync(permissions).ConfigureAwait(false)).ToList();
            var enumeratedPermissions = b2CPermissions.ToList();
            var permissionsToPass = enumeratedPermissions.Select(x => x.ToString()).ToList();
            try
            {
                var app = await CreateAppInB2CAsync(actorNumber.Value, permissionsToPass).ConfigureAwait(false);

                var servicePrincipal = await AddServicePrincipalToAppInB2CAsync(app.AppId!).ConfigureAwait(false);

                foreach (var permission in enumeratedPermissions)
                {
                    await GrantAddedRoleToServicePrincipalAsync(
                            servicePrincipal.Id!,
                            permission)
                        .ConfigureAwait(false);
                }

                return new CreateAppRegistrationResponse(
                    new ExternalActorId(Guid.Parse(app.AppId!)),
                    app.Id!,
                    servicePrincipal.Id!);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2CService)}");
                throw;
            }
        }

        public async Task DeleteAppRegistrationAsync(ExternalActorId externalActorId)
        {
            ArgumentNullException.ThrowIfNull(externalActorId);

            try
            {
                var foundApp = await GetExistingAppAsync(externalActorId).ConfigureAwait(false);
                if (foundApp == null)
                {
                    throw new InvalidOperationException("Cannot delete registration from B2C; Application was not found.");
                }

                await _graphClient.Applications[foundApp.Id]
                    .DeleteAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2CService)}");
                throw;
            }
        }

        public async Task<ActiveDirectoryAppInformation> GetExistingAppRegistrationAsync(
            AppRegistrationObjectId appRegistrationObjectId,
            AppRegistrationServicePrincipalObjectId appRegistrationServicePrincipalObjectId)
        {
            ArgumentNullException.ThrowIfNull(appRegistrationObjectId, nameof(appRegistrationObjectId));
            ArgumentNullException.ThrowIfNull(appRegistrationServicePrincipalObjectId, nameof(appRegistrationServicePrincipalObjectId));

            try
            {
                var retrievedApp = (await _graphClient.Applications[appRegistrationObjectId.Value.ToString()]
                    .GetAsync(x =>
                    {
                        x.QueryParameters.Select = new[]
                        {
                            "appId", "id", "displayName", "appRoles"
                        };
                    })
                    .ConfigureAwait(false))!;

                var appRoles = await GetRolesAsync(appRegistrationServicePrincipalObjectId.Value).ConfigureAwait(false);
                return new ActiveDirectoryAppInformation(
                    retrievedApp.AppId!,
                    retrievedApp.Id!,
                    retrievedApp.DisplayName!,
                    appRoles);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2CService)}");
                throw;
            }
        }

        public async Task<(Guid SecretId, string SecretText, DateTimeOffset ExpirationDate)> CreateSecretForAppRegistrationAsync(ExternalActorId externalActorId)
        {
            ArgumentNullException.ThrowIfNull(externalActorId);

            var foundApp = await GetExistingAppAsync(externalActorId).ConfigureAwait(false);
            if (foundApp == null)
            {
                throw new InvalidOperationException("Cannot add secret to B2C, Application was not found.");
            }

            var passwordCredential = new PasswordCredential
            {
                DisplayName = SecretDisplayName,
                StartDateTime = DateTimeOffset.Now,
                EndDateTime = DateTimeOffset.Now.AddMonths(6),
                KeyId = Guid.NewGuid(),
            };

            var secret = await _graphClient
                .Applications[foundApp.Id]
                .AddPassword
                .PostAsync(new AddPasswordPostRequestBody { PasswordCredential = passwordCredential })
                .ConfigureAwait(false);

            if (secret is { SecretText: not null, KeyId: not null, EndDateTime: not null })
            {
                return (secret.KeyId.Value, secret.SecretText, secret.EndDateTime.Value);
            }

            throw new InvalidOperationException($"Could not create secret in B2C for application {foundApp.AppId}");
        }

        public async Task RemoveSecretsForAppRegistrationAsync(ExternalActorId externalActorId)
        {
            ArgumentNullException.ThrowIfNull(externalActorId);

            var foundApp = await GetExistingAppAsync(externalActorId).ConfigureAwait(false);
            if (foundApp == null)
            {
                throw new InvalidOperationException("Cannot delete secrets from B2C; Application was not found.");
            }

            foreach (var secret in foundApp.PasswordCredentials!)
            {
                await _graphClient
                    .Applications[foundApp.Id]
                    .RemovePassword
                    .PostAsync(new RemovePasswordPostRequestBody { KeyId = secret.KeyId })
                    .ConfigureAwait(false);
            }
        }

        private async Task<Microsoft.Graph.Models.Application?> GetExistingAppAsync(ExternalActorId externalActorId)
        {
            var appId = externalActorId.Value.ToString();
            var applicationUsingAppId = await _graphClient
                .Applications
                .GetAsync(x => { x.QueryParameters.Filter = $"appId eq '{appId}'"; })
                .ConfigureAwait(false);

            var applications = await applicationUsingAppId!
                .IteratePagesAsync<Microsoft.Graph.Models.Application, ApplicationCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            return applications.SingleOrDefault();
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

        private async Task<IEnumerable<ActiveDirectoryRole>> GetRolesAsync(string servicePrincipalObjectId)
        {
            try
            {
                var response = await _graphClient.ServicePrincipals[servicePrincipalObjectId]
                    .AppRoleAssignments
                    .GetAsync()
                    .ConfigureAwait(false);

                var roles = await response!
                    .IteratePagesAsync<AppRoleAssignment, AppRoleAssignmentCollectionResponse>(_graphClient)
                    .ConfigureAwait(false);

                if (roles is null)
                {
                    throw new InvalidOperationException($"'{nameof(roles)}' is null");
                }

                var roleIds = new List<ActiveDirectoryRole>();
                foreach (var role in roles)
                {
                    roleIds.Add(new ActiveDirectoryRole(role.AppRoleId.ToString()!));
                }

                return roleIds;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2CService)}");
                throw;
            }
        }

        private async ValueTask GrantAddedRoleToServicePrincipalAsync(
            string consumerServicePrincipalObjectId,
            Guid roleId)
        {
            var appRole = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(consumerServicePrincipalObjectId),
                ResourceId = Guid.Parse(_azureAdConfig.BackendAppServicePrincipalObjectId),
                AppRoleId = roleId
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
            string consumerAppName,
            IReadOnlyList<string> permissions)
        {
            var resourceAccesses = permissions.Select(permission =>
                new ResourceAccess
                {
                    Id = Guid.Parse(permission),
                    Type = "Role"
                }).ToList();

            return (await _graphClient.Applications
                .PostAsync(new Microsoft.Graph.Models.Application
                {
                    DisplayName = consumerAppName,
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    RequiredResourceAccess = new List<RequiredResourceAccess>
                    {
                        new()
                        {
                            ResourceAppId = _azureAdConfig.BackendAppId,
                            ResourceAccess = resourceAccesses
                        }
                    },
                    SignInAudience = "AzureADMultipleOrgs"
                }).ConfigureAwait(false))!;
        }

        private async Task<ServicePrincipal> AddServicePrincipalToAppInB2CAsync(string consumerAppId)
        {
            var consumerServicePrincipal = new ServicePrincipal
            {
                AppId = consumerAppId
            };

            return (await _graphClient.ServicePrincipals
                .PostAsync(consumerServicePrincipal).ConfigureAwait(false))!;
        }
    }
}
