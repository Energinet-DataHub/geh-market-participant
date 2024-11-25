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

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Provides access to the Actor Consolidations.
/// </summary>
public interface IActorConsolidationRepository
{
    /// <summary>
    /// Adds an <see cref="ActorConsolidation"/>
    /// </summary>
    /// <param name="actorConsolidation">The actor to consolidate</param>
    /// <returns>The <see cref="ActorConsolidationId">id</see> of the added <see cref="ActorConsolidation"/></returns>
    /// <remarks>Throws an exception if the entity to add is not with a default GUID as id</remarks>
    Task<ActorConsolidationId> AddAsync(ActorConsolidation actorConsolidation);

    /// <summary>
    /// Gets a <see cref="ActorConsolidation"/> with the specified <see cref="ActorConsolidationId">id</see>.
    /// </summary>
    /// <param name="id">The <see cref="ActorConsolidationId">id</see> of the <see cref="ActorConsolidation"/> to get.</param>
    /// <returns>The specified <see cref="ActorConsolidation"/>; or null if not found.</returns>
    Task<ActorConsolidation?> GetAsync(ActorConsolidationId id);

    /// <summary>
    /// Gets a list of <see cref="ActorConsolidation"/> where the specified <see cref="ActorId">id</see> is either from or to.
    /// </summary>
    /// <param name="id">The <see cref="ActorId">id</see> of the <see cref="Actor">Actor</see> you want to get consolidations for.</param>
    /// <returns>A List of <see cref="ActorConsolidation">consolidations</see>; or empty list if none found.</returns>
    Task<IEnumerable<ActorConsolidation>>? GetByActorIdAsync(ActorId id);
}
