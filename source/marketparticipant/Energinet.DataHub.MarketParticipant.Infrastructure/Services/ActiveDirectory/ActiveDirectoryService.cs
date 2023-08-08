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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory
{
    public sealed class ActiveDirectoryService : IActiveDirectoryService
    {
        private const string ActorApplicationRegistrationDisplayNamePrefix = "Actor";

        private readonly GraphServiceClient _graphClient;

        public ActiveDirectoryService(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task DeleteActorAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            var actorApp = await GetAppAsync(actor).ConfigureAwait(false);
            if (actorApp is null)
            {
                return;
            }

            // Remove service Principal for actor application registration
            var appServicePrincipal = await GetServicePrincipalToAppAsync(actorApp.AppId!).ConfigureAwait(false);
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
        }

        public async Task<bool> AppExistsAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var app = await GetAppAsync(actor).ConfigureAwait(false);
            return app is not null;
        }

        public async Task<(string ExternalActorId, string ActorDisplayName)> CreateOrUpdateAppAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            var app = await EnsureAppAsync(actor).ConfigureAwait(false);
            await EnsureServicePrincipalToAppAsync(app).ConfigureAwait(false);
            return (app.AppId!, app.DisplayName!);
        }

        private static string GenerateActorDisplayName(Actor actor)
        {
            var lengthForActorName = $"{ActorApplicationRegistrationDisplayNamePrefix}_{actor.ActorNumber.Value}__{Guid.Empty}".Length;
            if (string.IsNullOrEmpty(actor.Name.Value))
            {
                if (lengthForActorName < 120)
                {
                    return $"{ActorApplicationRegistrationDisplayNamePrefix}_{actor.ActorNumber.Value}_{actor.Id}";
                }
            }

            var totalNameLength = actor.Name.Value.Length + lengthForActorName;
            var actorName = totalNameLength > 120
                ? new string(actor.Name.Value.Take(120 - lengthForActorName).ToArray())
                : actor.Name.Value;
            return $"{ActorApplicationRegistrationDisplayNamePrefix}_{actor.ActorNumber.Value}_{actorName}_{actor.Id}";
        }

        private async Task<Microsoft.Graph.Models.Application> EnsureAppAsync(Actor actor)
        {
            var app = await GetAppAsync(actor).ConfigureAwait(false);
            if (app is not null)
                return app;

            app = await CreateAppRegistrationAsync(actor).ConfigureAwait(false);
            return app;
        }

        private async Task<Microsoft.Graph.Models.Application?> GetAppAsync(Actor actor)
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

        private async Task<ServicePrincipal?> GetServicePrincipalToAppAsync(string appId)
        {
            var response = await _graphClient
                .ServicePrincipals
                .GetAsync(x =>
                {
                    x.QueryParameters.Filter = $"appId eq '{appId}'";
                })
                .ConfigureAwait(false);

            var servicePrincipals = await response!
                .IteratePagesAsync<ServicePrincipal, ServicePrincipalCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            return servicePrincipals.FirstOrDefault();
        }

        private async Task EnsureServicePrincipalToAppAsync(Microsoft.Graph.Models.Application app)
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
                return;
            }

            servicePrincipal = new ServicePrincipal
            {
                AppId = app.AppId
            };

            await _graphClient.ServicePrincipals
                .PostAsync(servicePrincipal)
                .ConfigureAwait(false);
        }

        private async Task<Microsoft.Graph.Models.Application> CreateAppRegistrationAsync(Actor actor)
        {
            var app = await _graphClient.Applications
                .PostAsync(new Microsoft.Graph.Models.Application
                {
                    DisplayName = GenerateActorDisplayName(actor),
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    SignInAudience = SignInAudience.AzureADMyOrg.ToString()
                }).ConfigureAwait(false);
            return app!;
        }
    }
}
