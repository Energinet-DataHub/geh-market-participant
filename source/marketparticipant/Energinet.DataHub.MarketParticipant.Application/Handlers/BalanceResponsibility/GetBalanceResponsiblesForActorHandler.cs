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

public sealed class GetBalanceResponsibilityAgreementsHandler : IRequestHandler<GetBalanceResponsibilityAgreementsCommand, GetBalanceResponsibilityAgreementsResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IBalanceResponsibilityAgreementsRepository _balanceResponsibilityAgreementsRepository;

    public GetBalanceResponsibilityAgreementsHandler(
        IActorRepository actorRepository,
        IBalanceResponsibilityAgreementsRepository balanceResponsibilityAgreementsRepository)
    {
        _actorRepository = actorRepository;
        _balanceResponsibilityAgreementsRepository = balanceResponsibilityAgreementsRepository;
    }

    public async Task<GetBalanceResponsibilityAgreementsResponse> Handle(GetBalanceResponsibilityAgreementsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        if (actor.MarketRoles.Any(mr => mr.Function == EicFunction.EnergySupplier))
        {
            var brpAgreements = await _balanceResponsibilityAgreementsRepository
                .GetForEnergySupplierAsync(actor.Id)
                .ConfigureAwait(false);

            return new GetBalanceResponsibilityAgreementsResponse(brpAgreements
                .SelectMany(agreements => agreements
                    .Agreements
                    .Where(r => r.EnergySupplier == actor.Id)
                    .Select(r => Map(agreements.BalanceResponsibleParty, r))));
        }

        if (actor.MarketRoles.Any(mr => mr.Function == EicFunction.BalanceResponsibleParty))
        {
            var agreements = await _balanceResponsibilityAgreementsRepository
                .GetForBalanceResponsiblePartyAsync(actor.Id)
                .ConfigureAwait(false);

            return new GetBalanceResponsibilityAgreementsResponse(agreements
                .Agreements
                .Select(r => Map(agreements.BalanceResponsibleParty, r)));
        }

        throw new ValidationException("The specified actor does not support balance responsibility agreements.")
            .WithErrorCode("balance_responsibility.unsupported_actor");
    }

    private static BalanceResponsibilityAgreementDto Map(ActorId balanceResponsibleId, BalanceResponsibilityAgreement agreement)
    {
        return new BalanceResponsibilityAgreementDto(
            agreement.EnergySupplier.Value,
            balanceResponsibleId.Value,
            agreement.GridArea.Value,
            agreement.MeteringPointType,
            agreement.ValidFrom.ToDateTimeOffset(),
            agreement.ValidTo?.ToDateTimeOffset());
    }
}
