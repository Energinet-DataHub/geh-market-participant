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
/// Provides access to actors.
/// </summary>
public interface IActorRepository
{
    /// <summary>
    /// Adds the given actor to the repository, or updates it, if it already exists.
    /// </summary>
    /// <param name="actor">The actor to add or update.</param>
    /// <returns>The id of the added/updated actor.</returns>
    Task<ActorId> AddOrUpdateAsync(Actor actor);

    /// <summary>
    /// Gets an actor by their internal id.
    /// </summary>
    /// <param name="actorId">The id of the actor.</param>
    /// <returns>An actor for the specified id; or null if not found.</returns>
    Task<Actor?> GetAsync(ActorId actorId);

    /// <summary>
    /// Gets all actors.
    /// </summary>
    /// <returns>A list of actors.</returns>
    Task<IEnumerable<Actor>> GetActorsAsync();

    /// <summary>
    /// Gets all actors with the specified ids.
    /// </summary>
    /// <param name="actorIds">The list of actor ids.</param>
    /// <returns>A list of actors.</returns>
    Task<IEnumerable<Actor>> GetActorsAsync(IEnumerable<ActorId> actorIds);

    /// <summary>
    /// Gets all actors for the specified organization.
    /// </summary>
    /// <param name="organizationId">The organization to get the actors for.</param>
    /// <returns>A list of actors.</returns>
    Task<IEnumerable<Actor>> GetActorsAsync(OrganizationId organizationId);
}
