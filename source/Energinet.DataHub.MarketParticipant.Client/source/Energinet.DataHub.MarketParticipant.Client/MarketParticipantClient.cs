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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Energinet.DataHub.MarketParticipant.Client.Results;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantClient : IMarketParticipantClient
    {
        private const string OrganizationsBaseUrl = "Organization";

        // private static readonly JsonSerializerOptions _defaultOptions = new()
        // {
        //     Converters = { new JsonStringEnumConverter() },
        //     PropertyNameCaseInsensitive = true
        // };
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

            var listAllOrganizationsResult = await response.GetJsonAsync<ListAllOrganizationsResult>()
                .ConfigureAwait(false);

            return listAllOrganizationsResult?.Organizations ?? Array.Empty<OrganizationDto>();
        }

        // public async Task<OrganizationDto> GetOrganizationAsync(Guid organizationId)
        // {
        //     var response = await _httpClient
        //         .GetAsync(new Uri(OrganizationsBaseUrl, UriKind.Relative))
        //         .ConfigureAwait(false);
        //
        //     var listAllOrganizationsResult = await response
        //         .EnsureSuccessStatusCode()
        //         .Content
        //         .ReadFromJsonAsync<ListAllOrganizationsResult>(_defaultOptions)
        //         .ConfigureAwait(false);
        //
        //     return listAllOrganizationsResult?.Organizations ?? Array.Empty<OrganizationDto>();
        // }
    }
}
