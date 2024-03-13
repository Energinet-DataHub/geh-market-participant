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

public sealed class MessageDelegationRepository : IMessageDelegationRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public MessageDelegationRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<IEnumerable<MessageDelegation>> GetForActorAsync(ActorId delegatedBy)
    {
        var messageDelegations = await _marketParticipantDbContext
            .MessageDelegations
            .Where(messageDelegation => messageDelegation.DelegatedByActorId == delegatedBy.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return messageDelegations.Select(Map);
    }

    public async Task<IEnumerable<MessageDelegation>> GetDelegatedToActorAsync(ActorId delegatedTo)
    {
        var messageDelegations = await _marketParticipantDbContext
            .MessageDelegations
            .Where(messageDelegation => messageDelegation.Delegations.Any(d => d.DelegatedToActorId == delegatedTo.Value))
            .ToListAsync()
            .ConfigureAwait(false);

        return messageDelegations.Select(Map);
    }

    public async Task<MessageDelegation?> GetForActorAsync(ActorId delegatedBy, DelegationMessageType messageType)
    {
        var messageDelegation = await _marketParticipantDbContext
            .MessageDelegations
            .SingleOrDefaultAsync(messageDelegation =>
                messageDelegation.DelegatedByActorId == delegatedBy.Value &&
                messageDelegation.MessageType == messageType)
            .ConfigureAwait(false);

        return messageDelegation == null ? null : Map(messageDelegation);
    }

    public async Task<MessageDelegationId> AddOrUpdateAsync(MessageDelegation messageDelegation)
    {
        ArgumentNullException.ThrowIfNull(messageDelegation);

        MessageDelegationEntity destination;

        if (messageDelegation.Id.Value == default)
        {
            destination = new MessageDelegationEntity
            {
                DelegatedByActorId = messageDelegation.DelegatedBy.Value,
                MessageType = messageDelegation.MessageType
            };

            if (!messageDelegation.Delegations.Any())
                throw new InvalidOperationException("Message delegation requires at least one delegation period.");
        }
        else
        {
            destination = await _marketParticipantDbContext
                .MessageDelegations
                .FindAsync(messageDelegation.Id.Value)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"Delegation '{messageDelegation.Id.Value}' is missing, even though it cannot be deleted.");

            // Check concurrency token to ensure the loaded entity has not changed since delegation was updated.
            if (destination.ConcurrencyToken != messageDelegation.ConcurrencyToken)
                throw new DbUpdateConcurrencyException($"Delegation '{messageDelegation.Id.Value}' was changed concurrently.");
        }

        foreach (var delegationPeriod in messageDelegation.Delegations)
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
        _marketParticipantDbContext.MessageDelegations.Update(destination);

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);

        return new MessageDelegationId(destination.Id);
    }

    private static MessageDelegation Map(MessageDelegationEntity messageDelegationEntity)
    {
        return new MessageDelegation(
            new MessageDelegationId(messageDelegationEntity.Id),
            new ActorId(messageDelegationEntity.DelegatedByActorId),
            messageDelegationEntity.MessageType,
            messageDelegationEntity.ConcurrencyToken,
            messageDelegationEntity.Delegations.Select(Map));
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
