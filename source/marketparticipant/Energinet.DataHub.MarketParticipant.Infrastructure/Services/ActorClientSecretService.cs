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
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Applications.Item.RemovePassword;
using Microsoft.Graph.Models;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public sealed class ActorClientSecretService : IActorClientSecretService
    {
        private const string SecretDisplayName = "B2C Login - Secret";
        private readonly GraphServiceClient _graphClient;

        public ActorClientSecretService(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task<(Guid SecretId, string SecretText, Instant ExpirationDate)> CreateSecretAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            ArgumentNullException.ThrowIfNull(actor.ExternalActorId);

            var foundApp = await GetApplicationRegistrationAsync(actor.ExternalActorId).ConfigureAwait(false);
            if (foundApp == null)
            {
                throw new InvalidOperationException("Cannot add secret to B2C; application was not found.");
            }

            var passwordCredential = new PasswordCredential
            {
                DisplayName = SecretDisplayName,
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddMonths(6),
                KeyId = Guid.NewGuid(),
            };

            var secret = await _graphClient
                .Applications[foundApp.Id]
                .AddPassword
                .PostAsync(new AddPasswordPostRequestBody
                {
                    PasswordCredential = passwordCredential,
                })
                .ConfigureAwait(false);

            if (secret is { SecretText: not null, KeyId: not null, EndDateTime: not null })
            {
                return (secret.KeyId.Value, secret.SecretText, secret.EndDateTime.Value.ToInstant());
            }

            throw new InvalidOperationException($"Could not create secret in B2C for application {foundApp.AppId}");
        }

        public async Task RemoveSecretAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            ArgumentNullException.ThrowIfNull(actor.ExternalActorId);

            var foundApp = await GetApplicationRegistrationAsync(actor.ExternalActorId).ConfigureAwait(false);
            if (foundApp == null)
            {
                throw new InvalidOperationException("Cannot delete secrets from B2C; Application was not found.");
            }

            foreach (var secret in foundApp.PasswordCredentials!)
            {
                await _graphClient
                    .Applications[foundApp.Id]
                    .RemovePassword
                    .PostAsync(new RemovePasswordPostRequestBody
                    {
                        KeyId = secret.KeyId,
                    })
                    .ConfigureAwait(false);
            }
        }

        private async Task<Microsoft.Graph.Models.Application?> GetApplicationRegistrationAsync(ExternalActorId externalActorId)
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
    }
}
