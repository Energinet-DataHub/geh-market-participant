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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.BalanceResponsibility;

public sealed class ValidateBalanceResponsibilitiesHandler : IRequestHandler<ValidateBalanceResponsibilitiesCommand>
{
    private readonly IClock _clock;
    private readonly IActorRepository _actorRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IBalanceResponsibilityRequestRepository _balanceResponsibilityRequestsRepository;
    private readonly IBalanceResponsibilityRelationsRepository _balanceResponsibilityRelationsRepository;

    public ValidateBalanceResponsibilitiesHandler(
        IClock clock,
        IActorRepository actorRepository,
        IDomainEventRepository domainEventRepository,
        IBalanceResponsibilityRequestRepository balanceResponsibilityRequestsRepository,
        IBalanceResponsibilityRelationsRepository balanceResponsibilityRelationsRepository)
    {
        _clock = clock;
        _actorRepository = actorRepository;
        _domainEventRepository = domainEventRepository;
        _balanceResponsibilityRequestsRepository = balanceResponsibilityRequestsRepository;
        _balanceResponsibilityRelationsRepository = balanceResponsibilityRelationsRepository;
    }

    public async Task Handle(ValidateBalanceResponsibilitiesCommand request, CancellationToken cancellationToken)
    {
        var allActors = (await _actorRepository
            .GetActorsAsync()
            .ConfigureAwait(false))
            .ToList();

        var notificationTargets = allActors
            .Where(actor =>
                actor.Status == ActorStatus.Active &&
                actor.MarketRoles.Any(mr => mr.Function == EicFunction.DataHubAdministrator))
            .Select(actor => actor.Id)
            .ToList();

        foreach (var actor in allActors)
        {
            if (actor.Status != ActorStatus.Active)
                continue;

            if (actor.MarketRoles.All(mr => mr.Function != EicFunction.EnergySupplier))
                continue;

            IEnumerable<BalanceResponsibilityContractor> contractors;

            try
            {
                contractors = await _balanceResponsibilityRelationsRepository
                    .GetForEnergySupplierAsync(actor.Id)
                    .ConfigureAwait(false);
            }
            catch (ValidationException)
            {
                // Reading the balance responsibility requests triggers validation of several rules.
                // If the rules are violated, a notification is always sent.
                await NotifyAsync(notificationTargets, actor.ActorNumber, false).ConfigureAwait(false);
                continue;
            }

            var relations = contractors
                .SelectMany(cont => cont.Relations)
                .Where(relation => relation.EnergySupplier == actor.Id);

            if (ShouldNotify(relations))
            {
                await NotifyAsync(notificationTargets, actor.ActorNumber, false).ConfigureAwait(false);
            }
        }

        var unrecognizedActors = await _balanceResponsibilityRequestsRepository
            .GetUnrecognizedActorsAsync()
            .ConfigureAwait(false);

        foreach (var actorNumber in unrecognizedActors)
        {
            await NotifyAsync(notificationTargets, actorNumber, true).ConfigureAwait(false);
        }
    }

    private bool ShouldNotify(IEnumerable<BalanceResponsibilityRelation> relations)
    {
        var now = _clock.GetCurrentInstant();

        var relationsPerGridAreaAndMpType = relations
            .GroupBy(relation => new { relation.GridArea, relation.MeteringPointType });

        foreach (var groupedRelations in relationsPerGridAreaAndMpType)
        {
            var activeRelation = groupedRelations.FirstOrDefault(relation => new Interval(relation.ValidFrom, relation.ValidTo).Contains(now));
            if (activeRelation?.ValidTo == null)
                continue;

            // If ValidTo has been assigned recently (but ValidTo is not today), then notifications are skipped.
            // This "cooldown period" is to give the system time to receive new relations entered by users.
            if (now - activeRelation.ValidToAssignedAt < Duration.FromHours(2) && activeRelation.ValidTo - now > Duration.FromDays(1))
                continue;

            var hasOpenEndedRelation = false;
            var latestValidTo = activeRelation.ValidTo;

            // The check is to go through all relations - from current to final - and make sure that:
            // - There are no gaps.
            // - The last relation is open-ended/has no ValidTo.
            foreach (var nextRelation in groupedRelations.OrderBy(relation => relation.ValidFrom).SkipWhile(relation => relation != activeRelation))
            {
                // There is an open-ended relation, but more relations are present.
                if (hasOpenEndedRelation)
                {
                    hasOpenEndedRelation = false;
                    break;
                }

                // There is a gap.
                if (nextRelation != activeRelation && nextRelation.ValidFrom != latestValidTo)
                    break;

                if (nextRelation.ValidTo == null)
                {
                    hasOpenEndedRelation = true;
                }

                latestValidTo = nextRelation.ValidTo;
            }

            if (!hasOpenEndedRelation)
                return true;
        }

        return false;
    }

    private async Task NotifyAsync(IEnumerable<ActorId> targets, ActorNumber affectedActor, bool isActorUnrecognized)
    {
        foreach (var target in targets)
        {
            await _domainEventRepository
                .EnqueueAsync(new BalanceResponsibilityValidationFailed(target, affectedActor, isActorUnrecognized))
                .ConfigureAwait(false);
        }
    }
}
