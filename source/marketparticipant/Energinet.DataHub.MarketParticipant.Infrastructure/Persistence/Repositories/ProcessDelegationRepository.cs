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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ProcessDelegationRepository(IMarketParticipantDbContext marketParticipantDbContext)
    : IProcessDelegationRepository
{
    public async Task<ProcessDelegation?> GetAsync(ProcessDelegationId id)
    {
        var query =
            from pd in marketParticipantDbContext.ProcessDelegations
            join actor in marketParticipantDbContext.Actors on pd.DelegatedByActorId equals actor.Id
            where pd.Id == id.Value
            select new { actor, pd };

        var processDelegation = await query
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        return processDelegation == null ? null : Map(processDelegation.actor, processDelegation.pd);
    }

    public async Task<IEnumerable<ProcessDelegation>> GetForActorAsync(ActorId delegatedBy)
    {
        var query =
            from pd in marketParticipantDbContext.ProcessDelegations
            join actor in marketParticipantDbContext.Actors on pd.DelegatedByActorId equals actor.Id
            where pd.DelegatedByActorId == delegatedBy.Value
            select new { actor, pd };

        var processDelegations = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return processDelegations.Select(processDelegation => Map(processDelegation.actor, processDelegation.pd));
    }

    public async Task<IEnumerable<ProcessDelegation>> GetDelegatedToActorAsync(ActorId delegatedTo)
    {
        var query =
            from pd in marketParticipantDbContext.ProcessDelegations
            join actor in marketParticipantDbContext.Actors on pd.DelegatedByActorId equals actor.Id
            where pd.Delegations.Any(d => d.DelegatedToActorId == delegatedTo.Value)
            select new { actor, pd };

        var processDelegations = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return processDelegations.Select(processDelegation => Map(processDelegation.actor, processDelegation.pd));
    }

    public async Task<ProcessDelegation?> GetForActorAsync(ActorId delegatedBy, DelegatedProcess process)
    {
        var query =
            from pd in marketParticipantDbContext.ProcessDelegations
            join actor in marketParticipantDbContext.Actors on pd.DelegatedByActorId equals actor.Id
            where pd.DelegatedByActorId == delegatedBy.Value && pd.DelegatedProcess == process
            select new { actor, pd };

        var processDelegation = await query
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        return processDelegation == null ? null : Map(processDelegation.actor, processDelegation.pd);
    }

    public async Task<ProcessDelegationId> AddOrUpdateAsync(ProcessDelegation processDelegation)
    {
        ArgumentNullException.ThrowIfNull(processDelegation);

        ProcessDelegationEntity destination;

        if (processDelegation.Id.Value == default)
        {
            destination = new ProcessDelegationEntity
            {
                DelegatedByActorId = processDelegation.DelegatedBy.Value,
                DelegatedProcess = processDelegation.Process
            };
        }
        else
        {
            destination = await marketParticipantDbContext
                .ProcessDelegations
                .FindAsync(processDelegation.Id.Value)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"Delegation '{processDelegation.Id.Value}' is missing, even though it cannot be deleted.");

            // Check concurrency token to ensure the loaded entity has not changed since delegation was updated.
            if (destination.ConcurrencyToken != processDelegation.ConcurrencyToken)
                throw new DbUpdateConcurrencyException($"Delegation '{processDelegation.Id.Value}' was changed concurrently.");
        }

        foreach (var delegationPeriod in processDelegation.Delegations)
        {
            DelegationPeriodEntity delegationPeriodEntity;

            if (delegationPeriod.Id.Value == default)
            {
                delegationPeriodEntity = new DelegationPeriodEntity
                {
                    DelegatedToActorId = delegationPeriod.DelegatedTo.Value,
                    GridAreaId = delegationPeriod.GridAreaId.Value,
                    StartsAt = delegationPeriod.StartsAt.ToDateTimeOffset()
                };

                destination.Delegations.Add(delegationPeriodEntity);
            }
            else
            {
                delegationPeriodEntity = destination
                    .Delegations
                    .Single(d => d.Id == delegationPeriod.Id.Value);
            }

            delegationPeriodEntity.StopsAt = delegationPeriod.StopsAt?.ToDateTimeOffset();
        }

        destination.ConcurrencyToken = Guid.NewGuid();
        marketParticipantDbContext.ProcessDelegations.Update(destination);

        await marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);

        return new ProcessDelegationId(destination.Id);
    }

    private static ProcessDelegation Map(ActorEntity actorEntity, ProcessDelegationEntity processDelegationEntity)
    {
        return new ProcessDelegation(
            new ProcessDelegationId(processDelegationEntity.Id),
            new ActorId(processDelegationEntity.DelegatedByActorId),
            actorEntity.MarketRoles.SelectMany(mr => mr.GridAreas).Select(ga => new GridAreaId(ga.GridAreaId)),
            processDelegationEntity.DelegatedProcess,
            processDelegationEntity.ConcurrencyToken,
            processDelegationEntity.Delegations.Select(Map));
    }

    private static DelegationPeriod Map(DelegationPeriodEntity delegationPeriodEntity)
    {
        return new DelegationPeriod(
            new DelegationPeriodId(delegationPeriodEntity.Id),
            new ActorId(delegationPeriodEntity.DelegatedToActorId),
            new GridAreaId(delegationPeriodEntity.GridAreaId),
            delegationPeriodEntity.StartsAt.ToInstant(),
            delegationPeriodEntity.StopsAt?.ToInstant());
    }
}
