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
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories
{
    /// <summary>
    /// Provides access to the User Role Templates.
    /// </summary>
    public interface IUserRoleTemplateRepository
    {
        /// <summary>
        /// Updates or adds a User Role Template, it adds it if it's not already present.
        /// </summary>
        /// <param name="userRoleTemplate">The User Role Template to add or update</param>
        /// <returns>The id of the added User Role Template</returns>
        Task<Guid> AddOrUpdateAsync(UserRoleTemplate userRoleTemplate);

        /// <summary>
        /// Gets a User Role Template with the specified Id
        /// </summary>
        /// <param name="id">The Id of the User Role Template to get.</param>
        /// <returns>The specified User Role Template or null if not found</returns>
        Task<UserRoleTemplate?> GetAsync(Guid id);

        /// <summary>
        /// Retrieves all User Role Templates
        /// </summary>
        /// <returns>Grid areas</returns>
        Task<IEnumerable<UserRoleTemplate>> GetAsync();

        /// <summary>
        /// Retrieves all User Role Templates For a specific market role
        /// </summary>
        /// <returns>User Role Templates</returns>
        Task<IEnumerable<UserRoleTemplate>> GetForMarketAsync();
    }
}
