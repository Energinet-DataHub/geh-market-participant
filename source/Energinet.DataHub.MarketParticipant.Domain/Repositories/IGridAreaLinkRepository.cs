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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories
{
    /// <summary>
    /// Provides access to the Grid Areas Links.
    /// </summary>
    public interface IGridAreaLinkRepository
    {
        /// <summary>
        /// Updates a Grid Area Link, or adds it if it's not already present.
        /// </summary>
        /// <param name="gridAreaLink">The GridAreaLink to add or update</param>
        /// <returns>The id of the added GridAreaLink</returns>
        Task<GridAreaLinkId> AddOrUpdateAsync(GridAreaLink gridAreaLink);

        /// <summary>
        /// Gets an GridAreaLink with the specified Id
        /// </summary>
        /// <param name="id">The Id of the GridAreaLink to get.</param>
        /// <returns>The specified grid area link or null if not found</returns>
        Task<GridAreaLink?> GetAsync(GridAreaLinkId id);
    }
}
