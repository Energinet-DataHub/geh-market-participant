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

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class ActorMarketRole
    {
        private static readonly HashSet<EicFunction> _rolesAllowingSingleGridAreaOnly = new()
        {
            EicFunction.GridAccessProvider
        };

        public ActorMarketRole(EicFunction eic, IEnumerable<ActorGridArea> gridAreas, string? comment)
        {
            GridAreas = gridAreas.ToList();
            Function = eic;
            Comment = comment;

            var isSingleGridAreaRole = _rolesAllowingSingleGridAreaOnly.Contains(eic);
            if (isSingleGridAreaRole && GridAreas.Count > 1)
            {
                throw new ValidationException($"Only one grid area can be assigned to role '{eic}'.");
            }
        }

        public ActorMarketRole(EicFunction eic, IEnumerable<ActorGridArea> gridAreas)
            : this(eic, gridAreas, null)
        {
        }

        public ActorMarketRole(EicFunction eic)
            : this(eic, Array.Empty<ActorGridArea>(), null)
        {
        }

        public IReadOnlyCollection<ActorGridArea> GridAreas { get; }
        public EicFunction Function { get; }
        public string? Comment { get; }
    }
}
