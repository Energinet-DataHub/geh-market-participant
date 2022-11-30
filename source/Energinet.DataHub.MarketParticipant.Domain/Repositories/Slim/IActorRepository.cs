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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Slim;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;

/// <summary>
/// Repository for specialized fast read-only access to actors.
/// </summary>
public interface IActorRepository
{
    /// <summary>
    /// Gets an actor directly by their external id.
    /// </summary>
    /// <param name="actorId">The id of the actor.</param>
    /// <returns>An actor for the specified id; or null if not found.</returns>
    Task<Actor?> GetActorAsync(Guid actorId);
}
