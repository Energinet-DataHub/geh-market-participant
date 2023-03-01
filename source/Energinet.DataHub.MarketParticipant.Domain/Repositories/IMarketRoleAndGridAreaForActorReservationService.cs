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

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Manages reservations of market roles and grid areas for an actor.
/// </summary>
public interface IMarketRoleAndGridAreaForActorReservationService
{
    /// <summary>
    /// Reserves the specified market role and grid area to the given actor.
    /// </summary>
    /// <param name="actorId">The actor to assign the reservation to.</param>
    /// <param name="marketRole">The market role to reserve.</param>
    /// <param name="gridAreaId">The grid area to reserve.</param>
    /// <returns>Returns true if the reservation was accepted; false if the reservation is already taken.</returns>
    Task<bool> TryReserveAsync(ActorId actorId, EicFunction marketRole, GridAreaId gridAreaId);

    /// <summary>
    /// Removes all existing reservation for the given actor.
    /// </summary>
    /// <param name="actorId">The actor to remove the reservations for.</param>
    Task RemoveAllReservationsAsync(ActorId actorId);
}
