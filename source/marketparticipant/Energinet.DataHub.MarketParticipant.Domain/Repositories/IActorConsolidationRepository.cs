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
    /// Adds or updates an <see cref="ActorConsolidation"/>.
    /// </summary>
    /// <param name="actorConsolidation">The actor to consolidate.</param>
    /// <returns>The <see cref="ActorConsolidationId">id</see> of the added <see cref="ActorConsolidation"/>.</returns>
    Task<ActorConsolidationId> AddOrUpdateAsync(ActorConsolidation actorConsolidation);

    /// <summary>
    /// Gets a List of all <see cref="ActorConsolidation"/>.
    /// </summary>
    /// <returns>A list of <see cref="ActorConsolidation"/>.</returns>
    Task<IEnumerable<ActorConsolidation>> GetAsync();

    /// <summary>
    /// Gets a <see cref="ActorConsolidation"/> with the specified <see cref="ActorConsolidationId">id</see>.
    /// </summary>
    /// <param name="id">The <see cref="ActorConsolidationId">id</see> of the <see cref="ActorConsolidation"/> to get.</param>
    /// <returns>The specified <see cref="ActorConsolidation"/>; or null if not found.</returns>
    Task<ActorConsolidation?> GetAsync(ActorConsolidationId id);

    /// <summary>
    /// Gets a list of <see cref="ActorConsolidation"/> that are ready to be consolidated.
    /// </summary>
    /// <returns>A <see cref="IEnumerable{ActorConsolidation}"/> that are ready to be consolidated.</returns>
    Task<IEnumerable<ActorConsolidation>> GetReadyToConsolidateAsync();
}
