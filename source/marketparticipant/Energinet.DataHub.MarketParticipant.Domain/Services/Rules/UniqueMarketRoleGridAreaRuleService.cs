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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules
{
    public sealed class UniqueMarketRoleGridAreaRuleService : IUniqueMarketRoleGridAreaRuleService
    {
        private static readonly IReadOnlySet<EicFunction> _marketRoleSet = new HashSet<EicFunction>
        {
            EicFunction.GridAccessProvider
        };

        private readonly IMarketRoleAndGridAreaForActorReservationService _marketRoleAndGridAreaForActorReservationService;

        public UniqueMarketRoleGridAreaRuleService(IMarketRoleAndGridAreaForActorReservationService marketRoleAndGridAreaForActorReservationService)
        {
            _marketRoleAndGridAreaForActorReservationService = marketRoleAndGridAreaForActorReservationService;
        }

        public async Task ValidateAndReserveAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));

            var actorMarketRoles = actor.MarketRoles.Where(x => _marketRoleSet.Contains(x.Function));

            await _marketRoleAndGridAreaForActorReservationService
                .RemoveAllReservationsAsync(actor.Id)
                .ConfigureAwait(false);

            foreach (var actorMarketRole in actorMarketRoles)
            {
                foreach (var gridArea in actorMarketRole.GridAreas)
                {
                    var couldReserve = await _marketRoleAndGridAreaForActorReservationService
                        .TryReserveAsync(actor.Id, actorMarketRole.Function, gridArea.Id)
                        .ConfigureAwait(false);

                    if (!couldReserve)
                    {
                        throw new ValidationException($"Another actor is already assigned the role of '{actorMarketRole.Function}' for the chosen grid area.")
                            .WithErrorCode("actor.grid_area.reserved")
                            .WithArgs(("market_role", actorMarketRole.Function), ("grid_area_id", gridArea.Id));
                    }
                }
            }
        }
    }
}
