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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;

/// <summary>
/// Repository for specialized fast read-only access to users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets all actors that are attached to the specified user through permissions.
    /// </summary>
    /// <param name="externalUserId">The external id of the user.</param>
    /// <returns>A list of actor ids attached to the specified user.</returns>
    Task<IEnumerable<Guid>> GetActorsAsync(ExternalUserId externalUserId);

    /// <summary>
    /// Gets all permissions a user has for the given actor.
    /// </summary>
    /// <param name="actorId">The id of the actor.</param>
    /// <param name="externalUserId">The external id of the user.</param>
    /// <returns>A list of the applicable permissions.</returns>
    Task<IEnumerable<Core.App.Common.Security.Permission>> GetPermissionsAsync(
        Guid actorId,
        ExternalUserId externalUserId);

    /// <summary>
    /// Checks whether the specified user under the specified actor is FAS
    /// </summary>
    /// <param name="actorId">The id of the actor.</param>
    /// <param name="externalUserId">The external id of the user.</param>
    /// <returns>Flag indicating whether the user under the actor is FAS</returns>
    Task<bool> IsFasAsync(
        Guid actorId,
        ExternalUserId externalUserId);
}
