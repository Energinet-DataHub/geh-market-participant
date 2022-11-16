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
    public interface IUserActorRepository
    {
        /// <summary>
        /// Updates or adds a UserActor, it adds it if it's not already present.
        /// </summary>
        /// <param name="userActor">The UserActor to add or update</param>
        Task AddOrUpdateAsync(UserActor userActor);

        /// <summary>
        /// Gets a UserACtor with the specified Id
        /// </summary>
        /// <param name="id">The Id of the UserActor to get.</param>
        /// <returns>The specified UserActor or null if not found</returns>
        Task<UserActor?> GetAsync(string id);

        /// <summary>
        /// Retrieves all UserActors
        /// </summary>
        /// <returns>UserActors</returns>
        Task<IEnumerable<UserActor>> GetAsync();
    }
}
