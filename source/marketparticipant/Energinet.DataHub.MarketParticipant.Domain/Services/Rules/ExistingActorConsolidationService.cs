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

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

public sealed class ExistingActorConsolidationService : IExistingActorConsolidationService
{
    private readonly IActorConsolidationRepository _actorConsolidationRepository;

    public ExistingActorConsolidationService(IActorConsolidationRepository organizationRepository)
    {
        _actorConsolidationRepository = organizationRepository;
    }

    public async Task CheckExistingConsolidationAsync(ActorId fromActorId, ActorId toActorId)
    {
        var existingConsolidations = (await _actorConsolidationRepository
            .GetAsync()
            .ConfigureAwait(false)).ToList();

        if (existingConsolidations.Any(consolidation => consolidation.ActorFromId == fromActorId || consolidation.ActorToId == fromActorId))
        {
            throw new ValidationException("The specified From actor has already been consolidated before.")
                .WithErrorCode("actor.consolidation.fromexists");
        }

        if (existingConsolidations.Any(consolidation => consolidation.ActorToId == fromActorId || consolidation.ActorFromId == toActorId))
        {
            throw new ValidationException("The specified From actor has an existing consolidation scheduled already.")
                .WithErrorCode("actor.consolidation.toexists");
        }
    }
}
