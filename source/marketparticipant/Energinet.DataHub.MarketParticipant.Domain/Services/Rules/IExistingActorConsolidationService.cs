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

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

/// <summary>
/// Ensures that there are no existing Consolidation for any of the actors chosen for a consolidation.
/// </summary>
public interface IExistingActorConsolidationService
{
    /// <summary>
    /// Checks whether either the from or to actor is already part of an existing consolidation, will throw an exception if they are.
    /// </summary>
    /// <param name="fromActorId">The <see cref="ActorId"/> of the discontinued actor.</param>
    /// <param name="toActorId">The <see cref="ActorId"/> of the surviving actor.</param>
    Task CheckExistingConsolidationAsync(ActorId fromActorId, ActorId toActorId);
}
