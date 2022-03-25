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
using Energinet.DataHub.MarketParticipant.Client.Models;

namespace Energinet.DataHub.MarketParticipant.Client
{
    /// <summary>
    /// BFF client for Energinet.DataHub.MarketParticipant.
    /// </summary>
    public interface IMarketParticipantClient
    {
        /// <summary>
        /// Gets all organizations.
        /// </summary>
        /// <returns>All organizations.</returns>
        Task<IEnumerable<OrganizationDto>> GetOrganizationsAsync();

        /// <summary>
        /// Gets a specific organization with the specified Id
        /// </summary>
        /// <param name="organizationId">The Id of the organization to get</param>
        /// <returns>The organization <see cref="OrganizationDto" /> with the specified id, or null if it wasn't found/></returns>
        Task<OrganizationDto?> GetOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Creates a new organization
        /// </summary>
        /// <param name="organizationDto"></param>
        /// <returns>The Id <see cref="Guid"/> of the created organization/></returns>
        Task<Guid?> CreateOrganizationAsync(ChangeOrganizationDto organizationDto);

        /// <summary>
        /// Updates an organization
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="organizationDto"></param>
        /// <returns>nothing</returns>
        Task<bool> UpdateOrganizationAsync(Guid organizationId, ChangeOrganizationDto organizationDto);

        /// <summary>
        /// Gets an actor
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="actorId"></param>
        /// <returns>An <see cref="ActorDto" /> actor or null if not found/></returns>
        Task<ActorDto?> GetActorAsync(Guid organizationId, Guid actorId);

        /// <summary>
        /// List all actors to an organization
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns>A List of Actors <see cref="ActorDto"/> belonging to the organization></returns>
        Task<IEnumerable<ActorDto>?> GetActorsAsync(Guid organizationId);

        /// <summary>
        /// Updates an actor
        /// </summary>
        /// <param name="organizationId">The organization the actor belongs to</param>
        /// <param name="actorId">The id of the actor to update</param>
        /// <param name="createActorDto">The data to update</param>
        /// <returns>True if the update was succesfull, otherwise false</returns>
        Task<bool?> UpdateActorAsync(Guid organizationId, Guid actorId, ChangeActorDto createActorDto);
    }
}
