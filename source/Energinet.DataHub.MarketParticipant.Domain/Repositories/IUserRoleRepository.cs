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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Provides access to user roles.
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    /// Returns all existing user roles
    /// </summary>
    /// <returns>The all existing user roles</returns>
    Task<IEnumerable<UserRoleInfo>> GetAllAsync();

    /// <summary>
    /// Gets the user role having the specified external id.
    /// </summary>
    /// <param name="userRoleId">The id of the user role.</param>
    /// <returns>The role if it exists; otherwise null.</returns>
    Task<UserRole?> GetAsync(UserRoleId userRoleId);

    /// <summary>
    /// Gets user roles that support the specified EIC-functions.
    /// </summary>
    /// <param name="eicFunctions">The list of EIC-functions the user role must support.</param>
    /// <returns>A list of user roles.</returns>
    Task<IEnumerable<UserRole>> GetAsync(IEnumerable<EicFunction> eicFunctions);
}
