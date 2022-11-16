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
    /// Provides access to Permissions
    /// </summary>
    public interface IPermissionRepository
    {
        /// <summary>
        /// Updates or adds a User Role Template, it adds it if it's not already present.
        /// </summary>
        /// <param name="permission">The Permission to add or update</param>
        Task AddOrUpdateAsync(Permission permission);

        /// <summary>
        /// Gets a Permission with the specified Id
        /// </summary>
        /// <param name="id">The Id of the Permission to get.</param>
        /// <returns>The specified Permission or null if not found</returns>
        Task<Permission?> GetAsync(string id);

        /// <summary>
        /// Retrieves all Permissions
        /// </summary>
        /// <returns>Permissions</returns>
        Task<IEnumerable<Permission>> GetAsync();
    }
}
