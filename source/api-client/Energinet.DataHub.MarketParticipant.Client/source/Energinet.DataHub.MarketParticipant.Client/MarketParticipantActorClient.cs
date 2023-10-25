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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Flurl.Http;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantActorClient : IMarketParticipantActorClient
    {
        private const string ActorBaseUrl = "Actor";

        private readonly IMarketParticipantClientFactory _clientFactory;

        public MarketParticipantActorClient(IMarketParticipantClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<ActorDto> GetActorAsync(Guid actorId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request(ActorBaseUrl, actorId)
                        .GetAsync())
                .ConfigureAwait(false);

            var actor = await response
                .GetJsonAsync<ActorDto>()
                .ConfigureAwait(false);

            return actor;
        }

        public async Task<IEnumerable<ActorDto>> GetActorsAsync()
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request(ActorBaseUrl)
                        .GetAsync())
                .ConfigureAwait(false);

            var actor = await response
                .GetJsonAsync<IEnumerable<ActorDto>>()
                .ConfigureAwait(false);

            return actor;
        }

        public async Task<Guid> CreateActorAsync(CreateActorDto createActorDto)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request(ActorBaseUrl)
                        .PostJsonAsync(createActorDto))
                .ConfigureAwait(false);

            var actor = await response
                .GetStringAsync()
                .ConfigureAwait(false);

            return Guid.Parse(actor);
        }

        public Task UpdateActorAsync(Guid actorId, ChangeActorDto changeActorDto)
        {
            return ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request(ActorBaseUrl, actorId)
                        .PutJsonAsync(changeActorDto));
        }

        public async Task<ActorAuditLogsDto> GetActorAuditLogsAsync(Guid actorId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request(ActorBaseUrl, actorId, "auditlogs")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<ActorAuditLogsDto>()
                .ConfigureAwait(false);
        }
    }
}
