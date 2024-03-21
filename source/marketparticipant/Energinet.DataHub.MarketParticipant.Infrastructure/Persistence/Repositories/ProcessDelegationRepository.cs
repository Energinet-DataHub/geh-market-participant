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
        var messageDelegation = await marketParticipantDbContext
            .ProcessDelegations
            .SingleOrDefaultAsync(messageDelegation => messageDelegation.Id == id.Value)
            .ConfigureAwait(false);

        return messageDelegation == null ? null : Map(messageDelegation);
    }

    public async Task<IEnumerable<ProcessDelegation>> GetForActorAsync(ActorId delegatedBy)
    {
        var messageDelegations = await marketParticipantDbContext
            .ProcessDelegations
            .Where(messageDelegation => messageDelegation.DelegatedByActorId == delegatedBy.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return messageDelegations.Select(Map);
    }

    public async Task<IEnumerable<ProcessDelegation>> GetDelegatedToActorAsync(ActorId delegatedTo)
    {
        var messageDelegations = await marketParticipantDbContext
            .ProcessDelegations
            .Where(messageDelegation => messageDelegation.Delegations.Any(d => d.DelegatedToActorId == delegatedTo.Value))
            .ToListAsync()
            .ConfigureAwait(false);

        return messageDelegations.Select(Map);
    }

    public async Task<ProcessDelegation?> GetForActorAsync(ActorId delegatedBy, DelegatedProcess process)
    {
        var messageDelegation = await marketParticipantDbContext
            .ProcessDelegations
            .SingleOrDefaultAsync(messageDelegation =>
                messageDelegation.DelegatedByActorId == delegatedBy.Value &&
                messageDelegation.DelegatedProcess == process)
            .ConfigureAwait(false);

        return messageDelegation == null ? null : Map(messageDelegation);
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

    private static ProcessDelegation Map(ProcessDelegationEntity processDelegationEntity)
    {
        return new ProcessDelegation(
            new ProcessDelegationId(processDelegationEntity.Id),
            new ActorId(processDelegationEntity.DelegatedByActorId),
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
