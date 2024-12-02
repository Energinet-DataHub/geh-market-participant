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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public sealed class ActorConsolidationService : IActorConsolidationService
{
    private readonly IActorConsolidationAuditLogRepository _actorConsolidationAuditLogRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IGridAreaRepository _gridAreaRepository;

    public ActorConsolidationService(
        IActorConsolidationAuditLogRepository actorConsolidationAuditLogRepository,
        IActorRepository actorRepository,
        IAuditIdentityProvider auditIdentityProvider,
        IDomainEventRepository domainEventRepository,
        IGridAreaRepository gridAreaRepository)
    {
        _actorConsolidationAuditLogRepository = actorConsolidationAuditLogRepository;
        _actorRepository = actorRepository;
        _auditIdentityProvider = auditIdentityProvider;
        _domainEventRepository = domainEventRepository;
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task ConsolidateAsync(ActorConsolidation actorConsolidation)
    {
        ArgumentNullException.ThrowIfNull(actorConsolidation);

        var fromActor = await _actorRepository.GetAsync(actorConsolidation.ActorFromId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(fromActor, actorConsolidation.ActorFromId.Value);

        var toActor = await _actorRepository.GetAsync(actorConsolidation.ActorToId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(toActor, actorConsolidation.ActorToId.Value);

        if (fromActor.MarketRole.Function is EicFunction.GridAccessProvider
            && toActor.MarketRole.Function is EicFunction.GridAccessProvider)
        {
            var actorGridAreasToTransfer = fromActor.MarketRole.GridAreas.ToList();

            fromActor.TransferGridAreasTo(toActor);

            await UpdateGridAreasValidToDateAsync(actorGridAreasToTransfer, actorConsolidation.ScheduledAt).ConfigureAwait(false);
            await AuditLogConsolidationCompletedAsync(actorGridAreasToTransfer, actorConsolidation).ConfigureAwait(false);
        }

        fromActor.Deactivate();

        await _actorRepository
            .AddOrUpdateAsync(fromActor)
            .ConfigureAwait(false);

        await _actorRepository
            .AddOrUpdateAsync(toActor)
            .ConfigureAwait(false);

        await _domainEventRepository
            .EnqueueAsync(fromActor)
            .ConfigureAwait(false);

        await _domainEventRepository
            .EnqueueAsync(toActor)
            .ConfigureAwait(false);
    }

    private async Task AuditLogConsolidationCompletedAsync(
        IEnumerable<ActorGridArea> actorGridAreas,
        ActorConsolidation actorConsolidation)
    {
        foreach (var actorGridArea in actorGridAreas)
        {
            await _actorConsolidationAuditLogRepository.AuditAsync(
                _auditIdentityProvider.IdentityId,
                GridAreaAuditedChange.ConsolidationCompleted,
                actorConsolidation,
                actorGridArea.Id).ConfigureAwait(false);
        }
    }

    private async Task UpdateGridAreasValidToDateAsync(ICollection<ActorGridArea> actorGridAreasToTransfer, Instant scheduledAt)
    {
        var allGridAreas = await _gridAreaRepository.GetAsync().ConfigureAwait(false);
        var gridAreasToUpdate = allGridAreas.Where(ga => actorGridAreasToTransfer.Select(aga => aga.Id).Contains(ga.Id));
        foreach (var gridArea in gridAreasToUpdate)
        {
            gridArea.ValidTo = scheduledAt.ToDateTimeOffset();
            await _gridAreaRepository
                .AddOrUpdateAsync(gridArea)
                .ConfigureAwait(false);
        }
    }
}
