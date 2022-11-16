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

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories
{
    /// <summary>
    /// Provides access to UserActors
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Updates or adds a User, it adds it if it's not already present.
        /// </summary>
        /// <param name="user">The User to add or update</param>
        Task AddOrUpdateAsync(User user);

        /// <summary>
        /// Gets a User with the specified Id
        /// </summary>
        /// <param name="id">The Id of the User to get.</param>
        /// <returns>The specified User or null if not found</returns>
        Task<UserActor?> GetAsync(string id);

        /// <summary>
        /// Retrieves all Users
        /// </summary>
        /// <returns>Users</returns>
        Task<IEnumerable<User>> GetAsync();
    }
}
