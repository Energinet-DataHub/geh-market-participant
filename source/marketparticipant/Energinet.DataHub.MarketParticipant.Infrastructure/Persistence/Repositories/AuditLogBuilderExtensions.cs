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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Audit;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public static class AuditLogBuilderExtensions
{
    public static AuditLogBuilder<TAuditedChange, TAuditedEntity> Add<TAuditedChange, TAuditedEntity>(
        this AuditLogBuilder<TAuditedChange, TAuditedEntity> builder,
        TAuditedChange change,
        Func<TAuditedEntity, object?> compareSelector)
            where TAuditedChange : Enum
            where TAuditedEntity : class, IAuditedEntity
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new AuditedChangeComparer<TAuditedChange, TAuditedEntity>(
            change,
            compareSelector,
            entity => compareSelector(entity)?.ToString(),
            AuditedChangeCompareAt.ChangeOnly));
    }

    public static AuditLogBuilder<TAuditedChange, TAuditedEntity> Add<TAuditedChange, TAuditedEntity>(
        this AuditLogBuilder<TAuditedChange, TAuditedEntity> builder,
        TAuditedChange change,
        Func<TAuditedEntity, object?> compareSelector,
        AuditedChangeCompareAt compareAt)
            where TAuditedChange : Enum
            where TAuditedEntity : class, IAuditedEntity
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new AuditedChangeComparer<TAuditedChange, TAuditedEntity>(
            change,
            compareSelector,
            entity => compareSelector(entity)?.ToString(),
            compareAt));
    }

    public static AuditLogBuilder<TAuditedChange, TAuditedEntity> Add<TAuditedChange, TAuditedEntity>(
        this AuditLogBuilder<TAuditedChange, TAuditedEntity> builder,
        TAuditedChange change,
        Func<TAuditedEntity, object?> compareSelector,
        Func<TAuditedEntity, string?> auditedValueSelector,
        AuditedChangeCompareAt compareAt)
            where TAuditedChange : Enum
            where TAuditedEntity : class, IAuditedEntity
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new AuditedChangeComparer<TAuditedChange, TAuditedEntity>(
            change,
            compareSelector,
            auditedValueSelector,
            compareAt));
    }

    public static AuditLogBuilder<TAuditedChange, TAuditedEntity> Add<TAuditedChange, TAuditedEntity>(
        this AuditLogBuilder<TAuditedChange, TAuditedEntity> builder,
        TAuditedChange change,
        AuditedChangeCompareAt compareAt,
        Func<TAuditedEntity, string?> auditedValueSelector)
            where TAuditedChange : Enum
            where TAuditedEntity : class, IAuditedEntity
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new AuditedChangeComparer<TAuditedChange, TAuditedEntity>(
            change,
            _ => null,
            auditedValueSelector,
            compareAt));
    }
}
