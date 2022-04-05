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
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public sealed class ActiveDirectoryB2cService : IActiveDirectoryService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly AzureAdConfig _azureAdConfig;
        private readonly ILogger<ActiveDirectoryB2cService> _logger;

        public ActiveDirectoryB2cService(
            GraphServiceClient graphClient,
            AzureAdConfig config,
            ILogger<ActiveDirectoryB2cService> logger)
        {
            _graphClient = graphClient;
            _azureAdConfig = config;
            _logger = logger;
        }

        public async Task<CreateAppRegistrationResponse> CreateAppRegistrationAsync(
            string consumerAppName,
            IReadOnlyList<string> permissions)
        {
            Guard.ThrowIfNull(consumerAppName, nameof(consumerAppName));
            Guard.ThrowIfNull(permissions, nameof(permissions));

            try
            {
                var app = await CreateConsumerAppInB2CAsync(consumerAppName, permissions).ConfigureAwait(false);

                var servicePrincipal = await AddServicePrincipalToConsumerAppInB2CAsync(app.AppId).ConfigureAwait(false);

                // What should be done with this role? To database? Integration event?
                foreach (var permission in permissions)
                {
                    await GrantAddedRoleToConsumerServicePrincipalAsync(
                        servicePrincipal.Id,
                        permission)
                        .ConfigureAwait(false);
                }

                return new CreateAppRegistrationResponse(
                    new ExternalActorId(app.AppId),
                    app.Id,
                    app.AppId,
                    servicePrincipal.Id);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
                throw;
            }
        }

        public async Task<AppRegistrationSecret> CreateSecretForAppRegistrationAsync(string appRegistrationObjectId)
        {
            var passwordCredential = new PasswordCredential
            {
                DisplayName = "App secret",
                StartDateTime = DateTimeOffset.Now,
                EndDateTime = DateTimeOffset.Now.AddMonths(6),
                KeyId = Guid.NewGuid(),
                CustomKeyIdentifier = null,
            };

            var secret = await _graphClient.Applications[appRegistrationObjectId]
                .AddPassword(passwordCredential)
                .Request()
                .PostAsync().ConfigureAwait(false);

            return new AppRegistrationSecret(secret.SecretText);
        }

        public async Task DeleteAppRegistrationAsync(string appId)
        {
            Guard.ThrowIfNull(appId, nameof(appId));

            try
            {
                await _graphClient.Applications[appId]
                    .Request()
                    .DeleteAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
                throw;
            }
        }

        public async Task<ActiveDirectoryAppInformation> GetExistingAppRegistrationAsync(
            string consumerAppObjectId,
            string consumerServicePrincipalObjectId)
        {
            Guard.ThrowIfNull(consumerAppObjectId, nameof(consumerAppObjectId));

            try
            {
                var retrievedApp = await _graphClient.Applications[consumerAppObjectId]
                    .Request()
                    .Select(a => new { a.AppId, a.Id, a.DisplayName, a.AppRoles })
                    .GetAsync().ConfigureAwait(false);

                var appRoles = await GetRolesAsync(consumerServicePrincipalObjectId).ConfigureAwait(false);

                return new ActiveDirectoryAppInformation(
                    retrievedApp.AppId,
                    retrievedApp.Id,
                    retrievedApp.DisplayName,
                    appRoles);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
                throw;
            }
        }

        private async Task<ActiveDirectoryRoles> GetRolesAsync(string servicePrincipalObjectId)
        {
            Guard.ThrowIfNull(servicePrincipalObjectId, nameof(servicePrincipalObjectId));
            try
            {
                var roles = await _graphClient.ServicePrincipals[servicePrincipalObjectId]
                    .AppRoleAssignments
                    .Request()
                    .GetAsync()
                    .ConfigureAwait(false);

                if (roles is null)
                {
                    throw new InvalidOperationException($"'{nameof(roles)}' is null");
                }

                var roleIds = new ActiveDirectoryRoles();
                foreach (var role in roles)
                {
                    roleIds.Roles.Add(new ActiveDirectoryRole(role.AppRoleId.ToString()!));
                }

                return roleIds;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
                throw;
            }
        }

        private async Task<AppRegistrationRole> GrantAddedRoleToConsumerServicePrincipalAsync(
            string consumerServicePrincipalObjectId,
            string roleId)
        {
            var appRole = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(consumerServicePrincipalObjectId),
                ResourceId = Guid.Parse(_azureAdConfig.BackendAppServicePrincipalObjectId),
                AppRoleId = Guid.Parse(roleId) // Id for the role to assign to the service principal
            };

            var role = await _graphClient.ServicePrincipals[consumerServicePrincipalObjectId]
                .AppRoleAssignedTo
                .Request()
                .AddAsync(appRole).ConfigureAwait(false);

            if (role is null)
            {
                throw new InvalidOperationException($"The object: '{nameof(role)}' is null.");
            }

            return new AppRegistrationRole(role.AppRoleId!.Value);
        }

        private async Task<Application> CreateConsumerAppInB2CAsync(
            string consumerAppName,
            IReadOnlyList<string> permissions)
        {
            var resourceAccesses = permissions.Select(permission =>
                new ResourceAccess
                {
                    Id = Guid.Parse(permission),
                    Type = "Role"
                }).ToList();

            return await _graphClient.Applications
                .Request()
                .AddAsync(new Application
                {
                    DisplayName = consumerAppName,
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2,
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    RequiredResourceAccess = new[]
                    {
                        new RequiredResourceAccess
                        {
                            ResourceAppId = _azureAdConfig.BackendAppId,
                            ResourceAccess = resourceAccesses.Select(resourceAccess => new ResourceAccess { Id = resourceAccess.Id, Type = "Role" }).ToList(),
                        }
                    },
                    SignInAudience = "AzureADMultipleOrgs"
                }).ConfigureAwait(false);
        }

        private async Task<ServicePrincipal> AddServicePrincipalToConsumerAppInB2CAsync(string consumerAppId)
        {
            var consumerServicePrincipal = new ServicePrincipal
            {
                AppId = consumerAppId
            };

            return await _graphClient.ServicePrincipals
                .Request()
                .AddAsync(consumerServicePrincipal).ConfigureAwait(false);
        }
    }
}
