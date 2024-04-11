// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class ProcessDelegationHelperService : IProcessDelegationHelperService
{
    private IProcessDelegationRepository _processDelegationRepository;

    public ProcessDelegationHelperService(IProcessDelegationRepository processDelegationRepository)
    {
        _processDelegationRepository = processDelegationRepository;
    }

    public async Task VerifyValidActorsForProcessDelegationAsync(
        Actor delegatedFrom,
        Actor delegatedTo,
        IEnumerable<DelegatedProcess> processDelegations,
        IEnumerable<Guid> gridAreas)
    {
        ArgumentNullException.ThrowIfNull(delegatedFrom);
        ArgumentNullException.ThrowIfNull(delegatedTo);

        var currentDelegations = await _processDelegationRepository
            .GetForActorAsync(delegatedTo.Id)
            .ConfigureAwait(false);

        var currentDelegationsToFromActor = await _processDelegationRepository
            .GetDelegatedToActorAsync(delegatedFrom.Id)
            .ConfigureAwait(false);

        if (currentDelegations.Any())
        {
            throw new ValidationException("Trying to delegate to an actor that already has delegations to another actor.")
                .WithErrorCode("process_delegation.actor_to_already_has_delegations");
        }

        if (currentDelegationsToFromActor.Any())
        {
            throw new ValidationException("Trying to delegate from an actor that already has a delegation assigned to them.")
                .WithErrorCode("process_delegation.actor_from_already_has_delegations");
        }
    }
}
