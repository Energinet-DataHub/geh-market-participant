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
    public sealed class MarketParticipantUserRoleClient : IMarketParticipantUserRoleClient
    {
        private readonly IMarketParticipantClientFactory _clientFactory;

        public MarketParticipantUserRoleClient(IMarketParticipantClientFactory factory)
        {
            _clientFactory = factory;
        }

        public async Task<IEnumerable<UserRoleDto>> GetAsync(Guid actorId, Guid userId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request($"actors/{actorId}/users/{userId}/roles")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<IEnumerable<UserRoleDto>>()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<UserRoleDto>> GetAssignableAsync(Guid actorId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request($"actors/{actorId}/roles")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<IEnumerable<UserRoleDto>>()
                .ConfigureAwait(false);
        }
    }
}
