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

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;

public static class HistoryAuditExtensions
{
    public static async Task<IReadOnlyList<(T Entity, DateTimeOffset PeriodStart)>> ReadAllHistoryForAsync<T>(this DbSet<T> target, Expression<Func<T, bool>> entitySelector)
        where T : class, IAuditedEntity
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(entitySelector);

        var historyTableName = target
            .GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(T))!
            .GetHistoryTableName();

        var latest = await target
            .Where(entitySelector)
            .Select(entity => new
            {
                Entity = entity,
                PeriodStart = EF.Property<DateTime>(entity, "PeriodStart"),
            })
            .SingleAsync()
            .ConfigureAwait(false);

        var allHistory = await target
            .FromSqlRaw($"SELECT * FROM dbo.{historyTableName}")
            .AsNoTracking()
            .Where(entitySelector)
            .Select(entity => new
            {
                Entity = entity,
                PeriodStart = EF.Property<DateTime>(entity, "PeriodStart"),
            })
            .OrderBy(entity => entity.Entity.Version)
            .ToListAsync()
            .ConfigureAwait(false);

        return allHistory
            .Append(latest)
            .Select(entity => (entity.Entity, new DateTimeOffset(entity.PeriodStart, TimeSpan.Zero)))
            .ToList();
    }
}
