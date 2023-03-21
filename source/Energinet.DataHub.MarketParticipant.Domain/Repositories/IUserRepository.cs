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
/// Provides access to Users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Adds the given User to the repository, or updates it, if it already exists.
    /// </summary>
    /// <param name="user">The User to add or update.</param>
    /// <returns>The id of the added User.</returns>
    Task<UserId> AddOrUpdateAsync(User user);

    /// <summary>
    /// Gets the user having the specified external id.
    /// </summary>
    /// <param name="externalUserId">The external id of the user.</param>
    /// <returns>The user if it exists; otherwise null.</returns>
    Task<User?> GetAsync(ExternalUserId externalUserId);

    /// <summary>
    /// Gets the user having the specified id.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <returns>The user if it exists; otherwise null.</returns>
    Task<User?> GetAsync(UserId userId);

    /// <summary>
    /// Gets the users having the specified user role
    /// </summary>
    /// <param name="userRoleId">The id of the user role.</param>
    /// <returns>The user if it exists; otherwise null.</returns>
    Task<IEnumerable<User>?> GetToUserRoleAsync(UserRoleId userRoleId);
}
