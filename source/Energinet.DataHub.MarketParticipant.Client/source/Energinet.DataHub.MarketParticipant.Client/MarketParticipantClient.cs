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
    public sealed class MarketParticipantClient : IMarketParticipantClient
    {
        private const string OrganizationsBaseUrl = "Organization";
        private const string ActorBaseUrl = "Actor";

        private readonly IFlurlClient _httpClient;

        public MarketParticipantClient(IFlurlClient client)
        {
            _httpClient = client;
        }

        public async Task<IEnumerable<OrganizationDto>> GetOrganizationsAsync()
        {
            var response = await _httpClient
                .Request(OrganizationsBaseUrl)
                .GetAsync()
                .ConfigureAwait(false);

            var listAllOrganizationsResult = await response.GetJsonAsync<IEnumerable<OrganizationDto>>()
                .ConfigureAwait(false);

            return listAllOrganizationsResult ?? Array.Empty<OrganizationDto>();
        }

        public async Task<OrganizationDto?> GetOrganizationAsync(Guid organizationId)
        {
            var response = await _httpClient
                .Request(OrganizationsBaseUrl, organizationId)
                .GetAsync()
                .ConfigureAwait(false);

            var singleOrganizationsResult = await response.GetJsonAsync<OrganizationDto>()
                .ConfigureAwait(false);

            return singleOrganizationsResult;
        }

        public async Task<Guid?> CreateOrganizationAsync(ChangeOrganizationDto organizationDto)
        {
            var response = await _httpClient
                .Request(OrganizationsBaseUrl)
                .PostJsonAsync(organizationDto)
                .ConfigureAwait(false);

            var orgId = await response.GetStringAsync()
                .ConfigureAwait(false);

            return Guid.Parse(orgId);
        }

        public async Task<bool> UpdateOrganizationAsync(Guid organizationId, ChangeOrganizationDto organizationDto)
        {
            await _httpClient
                .Request(OrganizationsBaseUrl, organizationId)
                .PutJsonAsync(organizationDto)
                .ConfigureAwait(false);

            return true;
        }

        public async Task<IEnumerable<ActorDto>?> GetActorsAsync(Guid organizationId)
        {
            var response = await _httpClient
                .Request(OrganizationsBaseUrl, organizationId, ActorBaseUrl)
                .GetAsync()
                .ConfigureAwait(false);

            var actors = await response.GetJsonAsync<IEnumerable<ActorDto>>()
                .ConfigureAwait(false);

            return actors;
        }

        public async Task<ActorDto?> GetActorAsync(Guid organizationId, Guid actorId)
        {
            var response = await _httpClient
                .Request(OrganizationsBaseUrl, organizationId, ActorBaseUrl, actorId)
                .GetAsync()
                .ConfigureAwait(false);

            var actor = await response.GetJsonAsync<ActorDto>()
                .ConfigureAwait(false);

            return actor;
        }

        public async Task<Guid?> CreateActorAsync(Guid organizationId, CreateActorDto createActorDto)
        {
            var response = await _httpClient
                .Request(OrganizationsBaseUrl, organizationId, ActorBaseUrl)
                .PostJsonAsync(createActorDto)
                .ConfigureAwait(false);

            var actor = await response.GetStringAsync()
                .ConfigureAwait(false);

            return Guid.Parse(actor);
        }

        public async Task<bool?> UpdateActorAsync(Guid organizationId, Guid actorId, ChangeActorDto createActorDto)
        {
            try
            {
                await _httpClient
                    .Request(OrganizationsBaseUrl, organizationId, ActorBaseUrl, actorId)
                    .PutJsonAsync(createActorDto)
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException e)
            {
                throw new ArgumentException($"Flurl Body: {e.Call.RequestBody}");
            }
            return true;
        }
    }
}
