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
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory
{
    public sealed class ActiveDirectoryService : IActiveDirectoryService
    {
        private const string ActorApplicationRegistrationDisplayNamePrefix = "Actor";

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
            var actorApp = await GetAppAsync(actor).ConfigureAwait(false);
            if (actorApp is null)
            {
                return;
            }

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

        public async Task<(string ExternalActorId, string ActorDisplayName)> CreateOrUpdateAppAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            var app = await EnsureAppAsync(actor).ConfigureAwait(false);
            await EnsureServicePrincipalToAppAsync(app).ConfigureAwait(false);
            return (app.AppId, app.DisplayName);
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
            return $"{ActorApplicationRegistrationDisplayNamePrefix}_{actor.ActorNumber.Value}_{actorName ?? "-"}_{actor.Id}";
        }

        private async Task<Microsoft.Graph.Application> EnsureAppAsync(Actor actor)
        {
            var app = await GetAppAsync(actor).ConfigureAwait(false);
            if (app is not null) return app;

            app = await CreateAppRegistrationAsync(actor).ConfigureAwait(false);
            if (app is null)
            {
                throw new MarketParticipantException($"Error creating app registration for {actor.Id}");
            }

            return app;
        }

        private async Task<Microsoft.Graph.Application?> GetAppAsync(Actor actor)
        {
         return (await _graphClient
                    .Applications
                    .Request()
                    .Filter($"displayName eq '{GenerateActorDisplayName(actor)}'")
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

        private async Task EnsureServicePrincipalToAppAsync(Microsoft.Graph.Application app)
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

        private async Task<Microsoft.Graph.Application?> CreateAppRegistrationAsync(Actor actor)
        {
            var app = await _graphClient.Applications
                .Request()
                .AddAsync(new Microsoft.Graph.Application
                {
                    DisplayName = GenerateActorDisplayName(actor),
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
