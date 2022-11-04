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
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using AppPermission = Energinet.DataHub.Core.App.Common.Security.Permission;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory
{
    public sealed class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly AzureAdConfig _azureAdConfig;
        private readonly IBusinessRoleCodeDomainService _businessRoleCodeDomainService;
        private readonly IActiveDirectoryB2CRolesProvider _activeDirectoryB2CRolesProvider;
        private readonly ILogger<ActiveDirectoryService> _logger;

        public ActiveDirectoryService(
            GraphServiceClient graphClient,
            AzureAdConfig config,
            IBusinessRoleCodeDomainService businessRoleCodeDomainService,
            IActiveDirectoryB2CRolesProvider activeDirectoryB2CRolesProvider,
            ILogger<ActiveDirectoryService> logger)
        {
            _graphClient = graphClient;
            _azureAdConfig = config;
            _businessRoleCodeDomainService = businessRoleCodeDomainService;
            _activeDirectoryB2CRolesProvider = activeDirectoryB2CRolesProvider;
            _logger = logger;
        }

        public async Task<IEnumerable<(string AppId, string DisplayName)>> ListActorsAsync()
        {
            var appsCollection = await _graphClient.Applications.Request().GetAsync().ConfigureAwait(false);
            var result = new List<(string AppId, string DisplayName)>();
            var pageIterator = PageIterator<Application>.CreatePageIterator(
                _graphClient,
                appsCollection,
                application =>
                {
                    result.Add((application.Id, application.DisplayName));
                    return true;
                });
            await pageIterator.IterateAsync().ConfigureAwait(false);

            var test = await _graphClient.ServicePrincipals.Request().GetAsync().ConfigureAwait(false);
            return result;
        }

        public async Task<IEnumerable<AppPermission>> GetPermissionsToAppRegistrationAsync(Guid appRegistrationId)
        {
            var app = await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .GetAsync()
                .ConfigureAwait(false);
            var result = new List<AppPermission>();
            if (app is null) return Enumerable.Empty<AppPermission>();
            foreach (var appRole in app.AppRoles)
            {
                if (Enum.TryParse<AppPermission>(appRole.Value, out var permission))
                {
                    result.Add(permission);
                }
            }

            return result;
        }

        public async Task UpdatePermissionsToAppAsync(Guid appRegistrationId, IEnumerable<string> appRoles)
        {
            var app = await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .GetAsync()
                .ConfigureAwait(false);
            var updated = new Application
            {
                AppRoles = appRoles.Select(x =>
                    new AppRole() { Id = Guid.NewGuid(), Value = x, DisplayName = x, Description = x })
            };
            // TODO: Update to use ID
            await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .UpdateAsync(updated)
                .ConfigureAwait(false);
        }

        public async Task DeleteActorAsync(string identifier)
        {
            await _graphClient.Applications[identifier]
                .Request()
                .DeleteAsync()
                .ConfigureAwait(false);
        }

        public async Task<string> CreateActorAsync(BusinessRegisterIdentifier identifier, string name)
        {
            ArgumentNullException.ThrowIfNull(identifier);

            try
            {
                var frontendApp = await GetFrontendAppAsync(Guid.Parse("2b914bca-26d7-4d9b-a22e-8305f3259097")).ConfigureAwait(false);
                if (frontendApp is null)
                {
                    throw new MarketParticipantException($"Error creating app registration for {identifier.Identifier}, Frontend App not found");
                }

                var app = await CreateAppRegistrationAsync(identifier, name).ConfigureAwait(false);
                if (app is null)
                {
                    throw new MarketParticipantException($"Error creating app registration for {identifier.Identifier}");
                }

                var test = await AddServicePrincipalToActorAsync(app).ConfigureAwait(false);
                await AddGraphApiRequiredAccessAsync(app).ConfigureAwait(false);
                await AddRequiredAccessToFrontendAsync(app, frontendApp).ConfigureAwait(false);

                var permissionGrant = new OAuth2PermissionGrant
                    {
                        ClientId = "73aa3bad-dec5-4bad-9d2d-e9a218997b89",
                        ConsentType = "AllPrincipals", //permissionGrant.PrincipalId = "principalId-value";
                        ResourceId = test,
                        Scope = $"actor.default"
                    };

                await _graphClient.Oauth2PermissionGrants
                    .Request()
                    .AddAsync(permissionGrant)
                    .ConfigureAwait(false);
                return app.AppId;
            }
            catch (Exception e) when (e is not MarketParticipantException)
            {
                throw new MarketParticipantException($"Error creating app registration for {identifier.Identifier}", e);
            }
        }

        private async Task<string> AddServicePrincipalToActorAsync(Application app)
        {
            var servicePrincipal = await AddServicePrincipalToAppAsync(app.AppId).ConfigureAwait(false);
            var appRole = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(servicePrincipal.Id),
                ResourceId = Guid.Parse("73aa3bad-dec5-4bad-9d2d-e9a218997b89"),
                AppRoleId = Guid.Empty
            };

            await _graphClient.ServicePrincipals[servicePrincipal.Id]
                .AppRoleAssignedTo
                .Request()
                .AddAsync(appRole).ConfigureAwait(false);

            return servicePrincipal.Id;
        }

        private async Task AddRequiredAccessToFrontendAsync(Application app, Application frontendApp)
        {
            var scopeId = await AddApiScopesToAppRegistrationAsync(app).ConfigureAwait(false);
            if (scopeId is null)
            {
                throw new MarketParticipantException($"Error creating app registration for {app.Id}, could not add Scope");
            }

            var requiredAccess = new RequiredResourceAccess()
            {
                ResourceAppId = app.AppId,
                ResourceAccess = new List<ResourceAccess>()
                {
                    new() { Id = scopeId, Type = "Scope" }
                },
            };

            var updatedFrontend = new Application();
            var accessList = new List<RequiredResourceAccess>();
            accessList.AddRange(frontendApp.RequiredResourceAccess);
            accessList.Add(requiredAccess);
            updatedFrontend.RequiredResourceAccess = accessList;
            await _graphClient
                .Applications[frontendApp.Id]
                .Request()
                .UpdateAsync(updatedFrontend)
                .ConfigureAwait(false);
        }

        private async Task AddGraphApiRequiredAccessAsync(Application app)
        {
            var requiredAccessGraph = new RequiredResourceAccess()
            {
                ResourceAppId = "00000003-0000-0000-c000-000000000000",
                ResourceAccess = new List<ResourceAccess>()
                {
                    new() { Id = Guid.Parse("e1fe6dd8-ba31-4d61-89e7-88639da4683d"), Type = "Scope" }
                },
            };

            var updatedApp = new Application();
            var accessListGraph = new List<RequiredResourceAccess>();
            accessListGraph.Add(requiredAccessGraph);
            updatedApp.RequiredResourceAccess = accessListGraph;
            await _graphClient
                .Applications[app.Id]
                .Request()
                .UpdateAsync(updatedApp)
                .ConfigureAwait(false);
        }

        private async Task<Application?> GetFrontendAppAsync(Guid frontendAppId)
        {
           return (await _graphClient
                .Applications
                .Request()
                .Filter($"appId eq '{frontendAppId}'")
                .GetAsync()
                .ConfigureAwait(false))
               .FirstOrDefault();
        }

        private async Task<ServicePrincipal> AddServicePrincipalToAppAsync(string appId)
        {
            var servicePrincipal = new ServicePrincipal
            {
                AppId = appId
            };

            return await _graphClient.ServicePrincipals
                .Request()
                .AddAsync(servicePrincipal).ConfigureAwait(false);
        }

        private async Task<Guid?> AddApiScopesToAppRegistrationAsync(Application? app)
        {
            ArgumentNullException.ThrowIfNull(app);

            try
            {
                var updated = new Application();
                if (app.IdentifierUris.ToList().Count == 0)
                {
                    updated.IdentifierUris = new string[]
                    {
                        $"api://{app.AppId}"
                    };
                }

                var appScopes = app.Api.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
                var newScope = new PermissionScope
                {
                    Id = Guid.NewGuid(),
                    AdminConsentDescription = "default scope",
                    AdminConsentDisplayName = "default scope",
                    IsEnabled = true,
                    Type = "Admin",
                    Value = "actor.default"
                };
                appScopes.Add(newScope);
                updated.Api = new ApiApplication
                {
                    Oauth2PermissionScopes = appScopes
                };
                await _graphClient.Applications[app.Id]
                    .Request()
                    .UpdateAsync(updated)
                    .ConfigureAwait(false);
                return newScope.Id;
            }
            catch (Exception e)
            {
                throw new MarketParticipantException($"Error adding API and Scope to app registration for AppId '{app.AppId}'", e);
            }
        }

        private async Task<Application?> CreateAppRegistrationAsync(BusinessRegisterIdentifier identifier, string name)
        {
            var app = await _graphClient.Applications
                .Request()
                .AddAsync(new Application
                {
                    DisplayName = $"Actor_{identifier.Identifier}_{name}",
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2,
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    SignInAudience = SignInAudience.AzureADMyOrg.ToString(),
                }).ConfigureAwait(false);
            return app;
        }

        //     public async Task<AppRegistrationSecret> CreateSecretForAppRegistrationAsync(AppRegistrationObjectId appRegistrationObjectId)
        //     {
        //         ArgumentNullException.ThrowIfNull(appRegistrationObjectId, nameof(appRegistrationObjectId));
        //         var passwordCredential = new PasswordCredential
        //         {
        //             DisplayName = "App secret",
        //             StartDateTime = DateTimeOffset.Now,
        //             EndDateTime = DateTimeOffset.Now.AddMonths(6),
        //             KeyId = Guid.NewGuid(),
        //             CustomKeyIdentifier = null,
        //         };
        //
        //         var secret = await _graphClient.Applications[appRegistrationObjectId.Value.ToString()]
        //             .AddPassword(passwordCredential)
        //             .Request()
        //             .PostAsync().ConfigureAwait(false);
        //
        //         return new AppRegistrationSecret(secret.SecretText);
        //     }
        //
        //     public async Task DeleteAppRegistrationAsync(ExternalActorId externalActorId)
        //     {
        //         ArgumentNullException.ThrowIfNull(externalActorId);
        //
        //         try
        //         {
        //             var appId = externalActorId.Value.ToString();
        //             var applicationUsingAppId = await _graphClient
        //                 .Applications
        //                 .Request()
        //                 .Filter($"appId eq '{appId}'")
        //                 .GetAsync()
        //                 .ConfigureAwait(false);
        //
        //             var foundApp = applicationUsingAppId.SingleOrDefault();
        //             if (foundApp == null)
        //             {
        //                 throw new InvalidOperationException("Cannot delete registration from B2C; Application was not found.");
        //             }
        //
        //             await _graphClient.Applications[foundApp.Id]
        //                 .Request()
        //                 .DeleteAsync()
        //                 .ConfigureAwait(false);
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
        //             throw;
        //         }
        //     }
   }
}
