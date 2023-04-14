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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Flurl.Http;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantUserOverviewClient : IMarketParticipantUserOverviewClient
    {
        private readonly IMarketParticipantClientFactory _clientFactory;

        public MarketParticipantUserOverviewClient(IMarketParticipantClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<UserOverviewResultDto> SearchUsersAsync(
            int pageNumber,
            int pageSize,
            UserOverviewSortProperty sortProperty,
            SortDirection sortDirection,
            UserOverviewFilterDto filter)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("useroverview", "users", "search")
                        .SetQueryParam("pageNumber", pageNumber)
                        .SetQueryParam("pageSize", pageSize)
                        .SetQueryParam("sortProperty", sortProperty)
                        .SetQueryParam("sortDirection", sortDirection)
                        .PostJsonAsync(filter))
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<UserOverviewResultDto>()
                .ConfigureAwait(false);
        }
    }
}