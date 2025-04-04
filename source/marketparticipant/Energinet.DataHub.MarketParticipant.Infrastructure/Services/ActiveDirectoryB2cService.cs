﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Infrastructure.Options;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class ActiveDirectoryB2CService : IActiveDirectoryB2CService
{
    private const string ActorApplicationRegistrationDisplayNamePrefix = "Actor";
    private readonly GraphServiceClient _graphClient;
    private readonly IOptions<AzureB2COptions> _azureAdOptions;
    private readonly IActiveDirectoryB2BRolesProvider _activeDirectoryB2BRolesProvider;

    public ActiveDirectoryB2CService(
        GraphServiceClient graphClient,
        IOptions<AzureB2COptions> azureAdOptions,
        IActiveDirectoryB2BRolesProvider activeDirectoryB2BRolesProvider)
    {
        _graphClient = graphClient;
        _azureAdOptions = azureAdOptions;
        _activeDirectoryB2BRolesProvider = activeDirectoryB2BRolesProvider;
    }

    public async Task AssignApplicationRegistrationAsync(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor, nameof(actor));

        var b2CPermissions = (await MapEicFunctionsToB2CIdsAsync([actor.MarketRole.Function]).ConfigureAwait(false)).ToList();
        var applicationRegistration = await EnsureApplicationRegistrationAsync(actor, b2CPermissions).ConfigureAwait(false);
        var servicePrincipal = await EnsureServicePrincipalToAppAsync(applicationRegistration).ConfigureAwait(false);

        foreach (var permission in b2CPermissions)
        {
            await GrantAddedRoleToServicePrincipalAsync(
                    servicePrincipal.Id!,
                    permission)
                .ConfigureAwait(false);
        }

        actor.ExternalActorId = new ExternalActorId(Guid.Parse(applicationRegistration.AppId!));
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

        return servicePrincipals.SingleOrDefault();
    }

    private async Task<Microsoft.Graph.Models.Application> EnsureApplicationRegistrationAsync(Actor actor, IEnumerable<Guid> permissions)
    {
        return await FindApplicationRegistrationAsync(actor).ConfigureAwait(false) ??
               await CreateAppInB2CAsync(actor, permissions).ConfigureAwait(false);
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

        return servicePrincipal ?? (await _graphClient.ServicePrincipals
            .PostAsync(new ServicePrincipal
            {
                AppId = app.AppId,
            })
            .ConfigureAwait(false))!;
    }

    private async Task<Microsoft.Graph.Models.Application?> FindApplicationRegistrationAsync(Actor actor)
    {
        if (actor.ExternalActorId != null)
        {
            var appId = actor.ExternalActorId.ToString();
            var applicationUsingAppId = await _graphClient
                .Applications
                .GetAsync(x => { x.QueryParameters.Filter = $"appId eq '{appId}'"; })
                .ConfigureAwait(false);

            var applicationsUsingAppId = await applicationUsingAppId!
                .IteratePagesAsync<Microsoft.Graph.Models.Application, ApplicationCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            var foundByAppId = applicationsUsingAppId.SingleOrDefault();
            if (foundByAppId != null)
                return foundByAppId;
        }

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

    private async ValueTask GrantAddedRoleToServicePrincipalAsync(
        string consumerServicePrincipalObjectId,
        Guid roleId)
    {
        var appRole = new AppRoleAssignment
        {
            PrincipalId = Guid.Parse(consumerServicePrincipalObjectId),
            ResourceId = Guid.Parse(_azureAdOptions.Value.BackendSpnObjectId),
            AppRoleId = roleId,
        };

        var appRoleAssignedTo = await _graphClient.ServicePrincipals[consumerServicePrincipalObjectId]
            .AppRoleAssignments
            .GetAsync().ConfigureAwait(false);

        var roles = await appRoleAssignedTo!
            .IteratePagesAsync<AppRoleAssignment, AppRoleAssignmentCollectionResponse>(_graphClient)
            .ConfigureAwait(false);

        if (roles.Any(x => x.AppRoleId == roleId))
        {
            return;
        }

        await _graphClient.ServicePrincipals[consumerServicePrincipalObjectId]
            .AppRoleAssignedTo
            .PostAsync(appRole).ConfigureAwait(false);
    }

    private async Task<Microsoft.Graph.Models.Application> CreateAppInB2CAsync(
        Actor actor,
        IEnumerable<Guid> permissions)
    {
        var resourceAccesses = permissions.Select(permission =>
            new ResourceAccess
            {
                Id = permission,
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
                        ResourceAppId = _azureAdOptions.Value.BackendId,
                        ResourceAccess = resourceAccesses,
                    },
                },
                SignInAudience = SignInAudience.AzureADMultipleOrgs.ToString(),
            }).ConfigureAwait(false))!;
    }
}
