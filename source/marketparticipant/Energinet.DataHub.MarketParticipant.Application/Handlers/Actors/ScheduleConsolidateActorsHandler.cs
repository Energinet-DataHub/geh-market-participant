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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class ScheduleConsolidateActorsHandler : IRequestHandler<ScheduleConsolidateActorsCommand>
{
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IActorConsolidationAuditLogRepository _actorConsolidationAuditLogRepository;
    private readonly IActorConsolidationRepository _actorConsolidationRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;

    public ScheduleConsolidateActorsHandler(
        IAuditIdentityProvider auditIdentityProvider,
        IActorConsolidationAuditLogRepository actorConsolidationAuditLogRepository,
        IActorConsolidationRepository actorConsolidationRepository,
        IDomainEventRepository domainEventRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IActorRepository actorRepository)
    {
        _auditIdentityProvider = auditIdentityProvider;
        _actorConsolidationAuditLogRepository = actorConsolidationAuditLogRepository;
        _actorConsolidationRepository = actorConsolidationRepository;
        _domainEventRepository = domainEventRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _actorRepository = actorRepository;
    }

    public async Task Handle(ScheduleConsolidateActorsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var fromActorId = new ActorId(request.FromActorId);
        var fromActor = await _actorRepository.GetAsync(fromActorId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(fromActor, request.FromActorId);

        var toActorId = new ActorId(request.Consolidation.ToActorId);
        var toActor = await _actorRepository.GetAsync(toActorId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(toActor, request.Consolidation.ToActorId);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        var allActors = await _actorRepository
            .GetActorsAsync()
            .ConfigureAwait(false);

        var notificationTargets = allActors
            .Where(actor => actor is
            {
                Status: ActorStatus.Active,
                MarketRole.Function: EicFunction.DataHubAdministrator,
            })
            .Select(actor => actor.Id)
            .ToList();

        notificationTargets.Add(fromActorId);
        notificationTargets.Add(toActorId);

        await using (uow.ConfigureAwait(false))
        {
            var actorConsolidation = new ActorConsolidation(
                fromActorId,
                toActorId,
                request.Consolidation.ConsolidateAt.ToInstant());

            await _actorConsolidationRepository
                .AddOrUpdateAsync(actorConsolidation)
                .ConfigureAwait(false);

            foreach (var gridArea in fromActor.MarketRole.GridAreas)
            {
                await _actorConsolidationAuditLogRepository
                    .AuditAsync(
                        _auditIdentityProvider.IdentityId,
                        GridAreaAuditedChange.ConsolidationRequested,
                        actorConsolidation,
                        gridArea.Id)
                    .ConfigureAwait(false);
            }

            foreach (var notificationTarget in notificationTargets)
            {
                await _domainEventRepository
                    .EnqueueAsync(new ActorConsolidationScheduled(notificationTarget, fromActor.ActorNumber, actorConsolidation.ConsolidateAt))
                    .ConfigureAwait(false);
            }

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
