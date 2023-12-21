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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Audit;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class AuditLogBuilder<TAuditedChange, TAuditedEntity>
    where TAuditedChange : Enum
    where TAuditedEntity : class, IAuditedEntity
{
    private readonly List<AuditedChangeComparer<TAuditedChange, TAuditedEntity>> _auditedChanges = new();
    private readonly IAuditedEntityDataSource<TAuditedEntity> _dataSource;
    private (TAuditedEntity, DateTimeOffset)? _initialEntity;
    private Func<TAuditedEntity, object?> _keySelector;

    public AuditLogBuilder(IAuditedEntityDataSource<TAuditedEntity> dataSource)
    {
        _dataSource = dataSource;
        _keySelector = _ => null;
    }

    public AuditLogBuilder<TAuditedChange, TAuditedEntity> Add(AuditedChangeComparer<TAuditedChange, TAuditedEntity> auditedChangeComparer)
    {
        _auditedChanges.Add(auditedChangeComparer);
        return this;
    }

    public AuditLogBuilder<TAuditedChange, TAuditedEntity> WithInitial(TAuditedEntity auditedEntity, Instant timestamp)
    {
        _initialEntity = (auditedEntity, timestamp.ToDateTimeOffset());
        return this;
    }

    public AuditLogBuilder<TAuditedChange, TAuditedEntity> WithGrouping<TKey>(Func<TAuditedEntity, TKey> keySelector)
    {
        _keySelector = entity => keySelector(entity);
        return this;
    }

    public async Task<IEnumerable<AuditLog<TAuditedChange>>> BuildAsync()
    {
        var changes = await _dataSource
            .ReadChangesAsync()
            .ConfigureAwait(false);

        if (_initialEntity != null)
        {
            changes = changes.Prepend(_initialEntity.Value);
        }

        var auditLogs = new List<AuditLog<TAuditedChange>>();

        foreach (var changeGroup in changes.GroupBy(entity => _keySelector(entity.Entity)))
        {
            var entityList = changeGroup
                .OrderBy(entity => entity.Timestamp)
                .ThenBy(entity => entity.Entity.Version)
                .ToList();

            for (var i = 0; i < entityList.Count; i++)
            {
                auditLogs.AddRange(i == 0 ? BuildCreationAuditLogs(entityList) : BuildAuditLogs(entityList, i));
            }
        }

        return auditLogs;
    }

    private IEnumerable<AuditLog<TAuditedChange>> BuildCreationAuditLogs(IReadOnlyList<(TAuditedEntity Entity, DateTimeOffset Timestamp)> entities)
    {
        var (entity, timestamp) = entities[0];

        return from auditChange in _auditedChanges
               where auditChange.CompareAt is AuditedChangeCompareAt.Creation or AuditedChangeCompareAt.BothCreationAndDeletion
               select new AuditLog<TAuditedChange>(
                   auditChange.Change,
                   timestamp.ToInstant(),
                   new AuditIdentity(entity.ChangedByIdentityId),
                   true,
                   auditChange.GetAuditedValue(entity),
                   null);
    }

    private IEnumerable<AuditLog<TAuditedChange>> BuildAuditLogs(IReadOnlyList<(TAuditedEntity Entity, DateTimeOffset Timestamp)> entities, int i)
    {
        var (previous, _) = entities[i - 1];
        var (current, timestamp) = entities[i];

        if (i == entities.Count - 1 && current is IDeletableAuditedEntity { DeletedByIdentityId: not null } deleted)
            return BuildDeletionAuditLogs((current, timestamp), deleted.DeletedByIdentityId.Value);

        return from auditChange in _auditedChanges
               where auditChange.HasChanges(previous, current)
               select new AuditLog<TAuditedChange>(
                   auditChange.Change,
                   timestamp.ToInstant(),
                   new AuditIdentity(current.ChangedByIdentityId),
                   false,
                   auditChange.GetAuditedValue(current),
                   auditChange.GetAuditedValue(previous));
    }

    private IEnumerable<AuditLog<TAuditedChange>> BuildDeletionAuditLogs((TAuditedEntity Entity, DateTimeOffset Timestamp) deletedEntity, Guid deletedBy)
    {
        var (entity, timestamp) = deletedEntity;

        return from auditChange in _auditedChanges
               where auditChange.CompareAt is AuditedChangeCompareAt.Deletion or AuditedChangeCompareAt.BothCreationAndDeletion
               select new AuditLog<TAuditedChange>(
                   auditChange.Change,
                   timestamp.ToInstant(),
                   new AuditIdentity(deletedBy),
                   false,
                   null,
                   auditChange.GetAuditedValue(entity));
    }
}
