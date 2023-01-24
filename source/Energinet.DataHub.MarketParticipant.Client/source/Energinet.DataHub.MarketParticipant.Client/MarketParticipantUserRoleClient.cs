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

        public async Task<IEnumerable<UserRoleDto>> GetAllAsync()
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user-roles")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<IEnumerable<UserRoleDto>>()
                .ConfigureAwait(false);
        }

        public async Task<UserRoleWithPermissionsDto> GetAsync(Guid userRoleId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request($"user-roles/{userRoleId}")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<UserRoleWithPermissionsDto>()
                .ConfigureAwait(false);
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

        public async Task<Guid> CreateAsync(CreateUserRoleDto userRoleDto)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request($"user-roles")
                        .PostJsonAsync(userRoleDto))
                .ConfigureAwait(false);

            var roleId = await response
                .GetStringAsync()
                .ConfigureAwait(false);

            return Guid.Parse(roleId);
        }

        public async Task<Guid> UpdateAsync(Guid userRoleId, UpdateUserRoleDto userRoleUpdateDto)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request($"user-roles/{userRoleId}")
                        .PutJsonAsync(userRoleUpdateDto))
                .ConfigureAwait(false);

            var roleIdUpdated = await response
                .GetStringAsync()
                .ConfigureAwait(false);

            return Guid.Parse(roleIdUpdated);
        }

        public async Task<IEnumerable<UserRoleAuditLogEntryDto>> GetUserRoleAuditLogsAsync(Guid userRoleId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user-roles", userRoleId, "auditlogentry")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<IEnumerable<UserRoleAuditLogEntryDto>>()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<SelectablePermissionsDto>> GetSelectablePermissionsAsync()
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user-roles", "permissions")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<IEnumerable<SelectablePermissionsDto>>()
                .ConfigureAwait(false);
        }
    }
}
