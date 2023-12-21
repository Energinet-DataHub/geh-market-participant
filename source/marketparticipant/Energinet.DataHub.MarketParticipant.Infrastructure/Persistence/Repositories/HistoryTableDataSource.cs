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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class HistoryTableDataSource<TAuditedEntity> : IAuditedEntityDataSource<TAuditedEntity>
    where TAuditedEntity : class, IAuditedEntity
{
    private readonly DbSet<TAuditedEntity> _dataSource;
    private readonly Expression<Func<TAuditedEntity, bool>> _wherePredicate;

    public HistoryTableDataSource(DbSet<TAuditedEntity> dataSource, Expression<Func<TAuditedEntity, bool>> wherePredicate)
    {
        _dataSource = dataSource;
        _wherePredicate = wherePredicate;
    }

    public async Task<IEnumerable<(TAuditedEntity Entity, DateTimeOffset Timestamp)>> ReadChangesAsync()
    {
        var historyTableName = _dataSource
            .GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(TAuditedEntity))!
            .GetHistoryTableName();

        var allCurrent = await _dataSource
            .Where(_wherePredicate)
            .Select(entity => new
            {
                Entity = entity,
                PeriodStart = EF.Property<DateTime>(entity, "PeriodStart"),
            })
            .ToListAsync()
            .ConfigureAwait(false);

        var allHistory = await _dataSource
            .FromSqlRaw($"SELECT * FROM dbo.{historyTableName}")
            .AsNoTracking()
            .Where(_wherePredicate)
            .Select(entity => new
            {
                Entity = entity,
                PeriodStart = EF.Property<DateTime>(entity, "PeriodStart"),
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return allHistory
            .Concat(allCurrent)
            .Select(entity => (entity.Entity, new DateTimeOffset(entity.PeriodStart, TimeSpan.Zero)));
    }
}
