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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Gives access to user identity information
/// </summary>
public interface IUserIdentityRepository
{
    /// <summary>
    /// Searches for users using a search text
    /// </summary>
    /// <param name="searchText">The text to search for</param>
    /// <param name="accountEnabled">Specifies whether the returned users should be active, inactive or both.</param>
    /// <returns>A List of users matching the search text</returns>
    /// <remarks>Currently searches Name, Phone and Email</remarks>
    Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText, bool? accountEnabled);

    /// <summary>
    /// Retrieves user identity for the provided external id.
    /// </summary>
    /// <param name="externalId">The external id of the user identity.</param>
    Task<UserIdentity> GetUserIdentityAsync(ExternalUserId externalId);

    /// <summary>
    /// Retrieves user identities for the provided set of external ids.
    /// </summary>
    /// <param name="externalIds">A set of external ids.</param>
    Task<IEnumerable<UserIdentity>> GetUserIdentitiesAsync(IEnumerable<ExternalUserId> externalIds);

    /// <summary>
    /// Creates a new external user identity.
    /// </summary>
    /// <param name="userIdentity">The user identity to create.</param>
    /// <returns>The id of the external user identity.</returns>
    Task<ExternalUserId> CreateAsync(UserIdentity userIdentity);
}
