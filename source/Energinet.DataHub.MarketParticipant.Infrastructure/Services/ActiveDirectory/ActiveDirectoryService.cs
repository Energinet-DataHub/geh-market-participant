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
        private const string ActorApplicationRegistrationDisplayNamePrefix = "Actor_";
        private const string ActorDefaultScopeValue = "actor.default";

        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<ActiveDirectoryService> _logger;

        public ActiveDirectoryService(
            GraphServiceClient graphClient,
            ILogger<ActiveDirectoryService> logger)
        {
            _graphClient = graphClient;
            _logger = logger;
        }

        public async Task DeleteActorAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            var frontendApp = await GetFrontendAppAsync().ConfigureAwait(false);
            var actorApp = await GetAppAsync(actor).ConfigureAwait(false);
            if (frontendApp is null)
            {
                throw new MarketParticipantException($"Error deleting app registration for {actor}, Frontend App not found");
            }

            if (actorApp is null)
            {
                throw new MarketParticipantException($"Error deleting app registration for {actor}, App not found");
            }

            // Remove required access from frontend
            var accessList = new List<RequiredResourceAccess>();
            accessList.AddRange(frontendApp.RequiredResourceAccess
                .Where(x => x.ResourceAppId != actorApp.AppId));

            var updatedFrontend = new Application
            {
                RequiredResourceAccess = accessList
            };
            await _graphClient
                .Applications[frontendApp.Id]
                .Request()
                .UpdateAsync(updatedFrontend)
                .ConfigureAwait(false);

            // Remove service Principal for actor application registration
            var appServicePrincipal = await GetServicePrincipalToAppAsync(actorApp.AppId).ConfigureAwait(false);
            if (appServicePrincipal is not null)
            {
                await _graphClient.ServicePrincipals[appServicePrincipal.Id]
                    .Request()
                    .DeleteAsync()
                    .ConfigureAwait(false);
            }

            // Remove actor application
            await _graphClient.Applications[actorApp.Id]
                .Request()
                .DeleteAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> AppExistsAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var app = await GetAppAsync(actor).ConfigureAwait(false);
            return app is not null;
        }

        public async Task<string> CreateOrUpdateAppAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);

            try
            {
                var frontendApp = await GetFrontendAppAsync().ConfigureAwait(false);
                if (frontendApp is null)
                {
                    throw new MarketParticipantException($"Error creating app registration for {actor.Id}, Frontend App not found");
                }

                var frontendServicePrincipal = await GetServicePrincipalToAppAsync(frontendApp.AppId).ConfigureAwait(false);
                if (frontendServicePrincipal is null)
                {
                    throw new MarketParticipantException($"Error creating app registration for {actor.Id}, Frontend App Service Principal not found");
                }

                var app = await EnsureAppAsync(actor).ConfigureAwait(false);
                await EnsureScopeIdAsync(app, actor).ConfigureAwait(false);
                await EnsureServicePrincipalToAppAsync(app).ConfigureAwait(false);
                return app.AppId;
            }
            catch (Exception e) when (e is not MarketParticipantException)
            {
                throw new MarketParticipantException($"Error creating app registration for {actor.Id}", e);
            }
        }

        private async Task EnsureScopeIdAsync(Application app, Actor actor)
        {
            Guid? scopeId;
            if (app.Api == null)
            {
                scopeId = await AddApiScopesToAppRegistrationAsync(app).ConfigureAwait(false);
            }
            else
            {
                if (app.Api.Oauth2PermissionScopes.All(x => x.Value != ActorDefaultScopeValue))
                {
                    scopeId = await AddApiScopesToAppRegistrationAsync(app).ConfigureAwait(false);
                }
                else
                {
                    scopeId = app.Api.Oauth2PermissionScopes.First(x => x.Value == ActorDefaultScopeValue).Id;
                }
            }
        }

        private async Task AddGraphApiRequiredAccessAsync(Application app)
        {
            var requiredAccessGraph = new RequiredResourceAccess()
            {
                ResourceAppId = "00000003-0000-0000-c000-000000000000", // This is the id of the GraphAPI Resource, don't know if this can be obtained programmatically
                ResourceAccess = new List<ResourceAccess>()
                {
                    new() { Id = Guid.Parse("e1fe6dd8-ba31-4d61-89e7-88639da4683d"), Type = "Scope" } // Id of the User.Read GraphAPI Delegated permission
                },
            };

            var updatedApp = new Application();
            var accessListGraph = new List<RequiredResourceAccess>
            {
                requiredAccessGraph
            };

            updatedApp.RequiredResourceAccess = accessListGraph;
            await _graphClient
                .Applications[app.Id]
                .Request()
                .UpdateAsync(updatedApp)
                .ConfigureAwait(false);
        }

        private async Task<Application?> GetFrontendAppAsync()
        {
           return (await _graphClient
                .Applications
                .Request()
                .Filter($"displayName eq 'FrontEnd'")
                .GetAsync()
                .ConfigureAwait(false))
               .FirstOrDefault();
        }

        private async Task<Application> EnsureAppAsync(Actor actor)
        {
            var app = await GetAppAsync(actor).ConfigureAwait(false);
            if (app is not null) return app;

            app = await CreateAppRegistrationAsync(actor.Id).ConfigureAwait(false);
            if (app is null)
            {
                throw new MarketParticipantException($"Error creating app registration for {actor.Id}");
            }

            return app;
        }

        private async Task<Application?> GetAppAsync(Actor actor)
        {
         return (await _graphClient
                    .Applications
                    .Request()
                    .Filter($"displayName eq '{ActorApplicationRegistrationDisplayNamePrefix}{actor.Id}'")
                    .GetAsync()
                    .ConfigureAwait(false))
                .FirstOrDefault();
        }

        private async Task<ServicePrincipal?> GetServicePrincipalToAppAsync(string appId)
        {
            return (await _graphClient
                    .ServicePrincipals
                    .Request()
                    .Filter($"appId eq '{appId}'")
                    .GetAsync()
                    .ConfigureAwait(false))
                .FirstOrDefault();
        }

        private async Task EnsureServicePrincipalToAppAsync(Application app)
        {
            var servicePrincipal = (await _graphClient.ServicePrincipals
                .Request()
                .Filter($"appId eq '{app.AppId}'")
                .GetAsync()
                .ConfigureAwait(false))
                .FirstOrDefault();

            if (servicePrincipal is not null)
            {
                return;
            }

            servicePrincipal = new ServicePrincipal
            {
                AppId = app.AppId
            };

            await _graphClient.ServicePrincipals
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

                var appScopes = app.Api?.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
                var newScope = new PermissionScope
                {
                    Id = Guid.NewGuid(),
                    AdminConsentDescription = "default scope",
                    AdminConsentDisplayName = "default scope",
                    IsEnabled = true,
                    Type = "Admin",
                    Value = ActorDefaultScopeValue
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

        private async Task<Application?> CreateAppRegistrationAsync(Guid identifier)
        {
            var app = await _graphClient.Applications
                .Request()
                .AddAsync(new Application
                {
                    DisplayName = $"{ActorApplicationRegistrationDisplayNamePrefix}{identifier}",
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2,
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    SignInAudience = SignInAudience.AzureADMyOrg.ToString(),
                }).ConfigureAwait(false);
            return app;
        }
   }
}
