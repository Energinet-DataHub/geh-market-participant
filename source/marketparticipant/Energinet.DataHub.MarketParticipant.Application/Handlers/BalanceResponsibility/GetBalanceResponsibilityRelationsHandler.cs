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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.BalanceResponsibility;

public sealed class GetBalanceResponsibilityRelationsHandler : IRequestHandler<GetBalanceResponsibilityRelationsCommand, GetBalanceResponsibilityRelationsResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IBalanceResponsibilityRelationsRepository _balanceResponsibilityRelationsRepository;

    public GetBalanceResponsibilityRelationsHandler(
        IActorRepository actorRepository,
        IBalanceResponsibilityRelationsRepository balanceResponsibilityRelationsRepository)
    {
        _actorRepository = actorRepository;
        _balanceResponsibilityRelationsRepository = balanceResponsibilityRelationsRepository;
    }

    public async Task<GetBalanceResponsibilityRelationsResponse> Handle(GetBalanceResponsibilityRelationsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        if (actor.MarketRole is { Function: EicFunction.EnergySupplier })
        {
            var contractors = await _balanceResponsibilityRelationsRepository
                .GetForEnergySupplierAsync(actor.Id)
                .ConfigureAwait(false);

            return new GetBalanceResponsibilityRelationsResponse(contractors
                .SelectMany(contractor => contractor
                    .Relations
                    .Where(relation => relation.EnergySupplier == actor.Id)
                    .Select(relation => Map(contractor.BalanceResponsibleParty, relation))));
        }

        if (actor.MarketRole is { Function: EicFunction.BalanceResponsibleParty })
        {
            var relations = await _balanceResponsibilityRelationsRepository
                .GetForBalanceResponsiblePartyAsync(actor.Id)
                .ConfigureAwait(false);

            return new GetBalanceResponsibilityRelationsResponse(relations
                .Relations
                .Select(relation => Map(relations.BalanceResponsibleParty, relation)));
        }

        throw new ValidationException("The specified actor does not support balance responsibility relations.")
            .WithErrorCode("balance_responsibility.unsupported_actor");
    }

    private static BalanceResponsibilityRelationDto Map(ActorId balanceResponsibleId, BalanceResponsibilityRelation relation)
    {
        return new BalanceResponsibilityRelationDto(
            relation.EnergySupplier.Value,
            balanceResponsibleId.Value,
            relation.GridArea.Value,
            relation.MeteringPointType,
            relation.ValidFrom.ToDateTimeOffset(),
            relation.ValidTo?.ToDateTimeOffset());
    }
}
