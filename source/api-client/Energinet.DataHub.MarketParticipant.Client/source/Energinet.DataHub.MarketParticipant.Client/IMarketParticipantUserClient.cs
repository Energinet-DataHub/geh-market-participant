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

namespace Energinet.DataHub.MarketParticipant.Client
{
    /// <summary>
    /// Manages users.
    /// </summary>
    public interface IMarketParticipantUserClient
    {
        /// <summary>
        /// Gets actors assigned to the owner of the specified access token.
        /// </summary>
        Task<GetAssociatedUserActorsResponseDto> GetUserActorsAsync(string accessToken);

        /// <summary>
        /// Gets actors assigned to the userId specified
        /// </summary>
        Task<GetAssociatedUserActorsResponseDto> GetUserActorsAsync(Guid userId);

        /// <summary>
        /// Gets the specified user.
        /// </summary>
        Task<UserDto> GetUserAsync(Guid userId);

        /// <summary>
        /// Gets audit logs for the specified user.
        /// </summary>
        Task<UserAuditLogsDto> GetUserAuditLogsAsync(Guid userId);

        /// <summary>
        /// Update phone number for user identity
        /// </summary>
        /// <param name="userId">user to update</param>
        /// <param name="userIdentityUpdateDto">update values</param>
        Task UpdateUserPhoneNumberAsync(Guid userId, UserIdentityUpdateDto userIdentityUpdateDto);

        /// <summary>
        /// Initiates MitID user signup
        /// </summary>
        Task InitiateMitIdSignupAsync();
    }
}
