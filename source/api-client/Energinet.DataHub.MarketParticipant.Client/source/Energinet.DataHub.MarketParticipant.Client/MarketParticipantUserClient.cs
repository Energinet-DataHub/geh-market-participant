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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Flurl.Http;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantUserClient : IMarketParticipantUserClient
    {
        private readonly IMarketParticipantClientFactory _clientFactory;

        public MarketParticipantUserClient(IMarketParticipantClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<GetAssociatedUserActorsResponseDto> GetUserActorsAsync(string accessToken)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user/actors")
                        .SetQueryParam("externalToken", accessToken)
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<GetAssociatedUserActorsResponseDto>()
                .ConfigureAwait(false);
        }

        public async Task<GetAssociatedUserActorsResponseDto> GetUserActorsAsync(Guid userId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", userId, "actors")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<GetAssociatedUserActorsResponseDto>()
                .ConfigureAwait(false);
        }

        public async Task<UserDto> GetUserAsync(Guid userId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", userId)
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<UserDto>()
                .ConfigureAwait(false);
        }

        public async Task<UserAuditLogsDto> GetUserAuditLogsAsync(Guid userId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", userId, "auditlogentry")
                        .GetAsync())
                .ConfigureAwait(false);

            return await response
                .GetJsonAsync<UserAuditLogsDto>()
                .ConfigureAwait(false);
        }

        public async Task UpdateUserPhoneNumberAsync(
            Guid userId,
            UserIdentityUpdateDto userIdentityUpdateDto)
        {
            await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", userId, "useridentity")
                        .PutJsonAsync(userIdentityUpdateDto))
                .ConfigureAwait(false);
        }

        public async Task InitiateMitIdSignupAsync()
        {
            await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", "initiate-mitid-signup")
                        .PostAsync())
                .ConfigureAwait(false);
        }

        public async Task DeactivateUserAsync(Guid userId)
        {
            await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", userId, "deactivate")
                        .PutAsync())
                .ConfigureAwait(false);
        }

        public async Task ResetTotpTwoFactorAuthenticationAsync(Guid userId)
        {
            await ValidationExceptionHandler
                .HandleAsync(
                    () => _clientFactory
                        .CreateClient()
                        .Request("user", userId, "reset-totp-2fa")
                        .PutAsync())
                .ConfigureAwait(false);
        }
    }
}
