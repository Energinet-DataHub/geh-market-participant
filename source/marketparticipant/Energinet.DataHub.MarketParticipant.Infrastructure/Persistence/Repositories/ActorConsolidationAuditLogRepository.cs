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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorConsolidationAuditLogRepository : IActorConsolidationAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public ActorConsolidationAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<GridAreaAuditedChange>>> GetAsync(GridAreaId gridAreaId)
    {
        var entities = await _context
            .ActorConsolidationAuditLogEntries
            .Where(log => log.GridAreaId == gridAreaId.Value)
            .OrderBy(log => log.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        return
            from entity in entities
            let change = Map((ActorConsolidationAuditLogField)entity.Field)
            select new AuditLog<GridAreaAuditedChange>(
                change,
                entity.Timestamp.ToInstant(),
                new AuditIdentity(entity.ChangedByUserId),
                false,
                GetSerializedActorConsolidationActorAndDate(entity.ConsolidateAt, entity.NewValue),
                GetSerializedActorConsolidationActorAndDate(entity.ConsolidateAt, entity.OldValue));

        static GridAreaAuditedChange Map(ActorConsolidationAuditLogField field)
        {
            return field switch
            {
                ActorConsolidationAuditLogField.ConsolidationRequested => GridAreaAuditedChange.ConsolidationRequested,
                ActorConsolidationAuditLogField.ConsolidationCompleted => GridAreaAuditedChange.ConsolidationCompleted,
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };
        }
    }

    public async Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetAsync(ActorId actorId)
    {
        var entities = await _context
            .ActorConsolidationAuditLogEntries
            .Where(log => log.NewValue == actorId.Value.ToString() || log.OldValue == actorId.Value.ToString())
            .OrderBy(log => log.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var distinctEntities = entities
            .DistinctBy(log => new { log.OldValue, log.NewValue });

        return
            from entity in distinctEntities
            let change = Map((ActorConsolidationAuditLogField)entity.Field)
            select new AuditLog<ActorAuditedChange>(
                change,
                entity.Timestamp.ToInstant(),
                new AuditIdentity(entity.ChangedByUserId),
                false,
                GetSerializedActorConsolidationActorAndDate(entity.ConsolidateAt, entity.NewValue),
                GetSerializedActorConsolidationActorAndDate(entity.ConsolidateAt, entity.OldValue));

        static ActorAuditedChange Map(ActorConsolidationAuditLogField field)
        {
            return field switch
            {
                ActorConsolidationAuditLogField.ConsolidationRequested => ActorAuditedChange.ConsolidationRequested,
                ActorConsolidationAuditLogField.ConsolidationCompleted => ActorAuditedChange.ConsolidationCompleted,
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };
        }
    }

    public Task AuditAsync(
        AuditIdentity auditIdentity,
        GridAreaAuditedChange change,
        ActorConsolidation actorConsolidation,
        GridAreaId gridAreaId)
    {
        ArgumentNullException.ThrowIfNull(auditIdentity);
        ArgumentNullException.ThrowIfNull(actorConsolidation);
        ArgumentNullException.ThrowIfNull(gridAreaId);

        var entity = new ActorConsolidationAuditLogEntryEntity
        {
            GridAreaId = gridAreaId.Value,
            Timestamp = DateTimeOffset.UtcNow,
            ChangedByUserId = auditIdentity.Value,
            Field = (int)Map(change),
            NewValue = actorConsolidation.ActorToId.ToString(),
            OldValue = actorConsolidation.ActorFromId.ToString(),
            ConsolidateAt = actorConsolidation.ConsolidateAt.ToDateTimeOffset()
        };

        _context.ActorConsolidationAuditLogEntries.Add(entity);
        return _context.SaveChangesAsync();

        static ActorConsolidationAuditLogField Map(GridAreaAuditedChange change)
        {
            return change switch
            {
                GridAreaAuditedChange.ConsolidationRequested => ActorConsolidationAuditLogField.ConsolidationRequested,
                GridAreaAuditedChange.ConsolidationCompleted => ActorConsolidationAuditLogField.ConsolidationCompleted,
                _ => throw new ArgumentOutOfRangeException(nameof(change)),
            };
        }
    }

    private static string GetSerializedActorConsolidationActorAndDate(DateTimeOffset consolidateAt, string actorId)
    {
        return JsonSerializer.Serialize(new ActorConsolidationActorAndDate
        {
            ActorId = Guid.Parse(actorId),
            ConsolidateAt = consolidateAt
        });
    }
}
