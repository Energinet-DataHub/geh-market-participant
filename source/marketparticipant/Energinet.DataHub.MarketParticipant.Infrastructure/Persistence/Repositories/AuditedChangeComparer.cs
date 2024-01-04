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

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class AuditedChangeComparer<TAuditedChange, TAuditedEntity>
    where TAuditedChange : Enum
    where TAuditedEntity : class
{
    private readonly Func<TAuditedEntity, object?> _compareSelector;
    private readonly Func<TAuditedEntity, string?> _auditedValueSelector;

    public AuditedChangeComparer(
        TAuditedChange change,
        Func<TAuditedEntity, object?> compareSelector,
        Func<TAuditedEntity, string?> auditedValueSelector,
        AuditedChangeCompareAt compareAt)
    {
        Change = change;
        _compareSelector = compareSelector;
        _auditedValueSelector = auditedValueSelector;
        CompareAt = compareAt;
    }

    public TAuditedChange Change { get; }
    public AuditedChangeCompareAt CompareAt { get; }

    public bool HasChanges(TAuditedEntity previous, TAuditedEntity current)
    {
        return !Equals(_compareSelector(previous), _compareSelector(current));
    }

    public string? GetAuditedValue(TAuditedEntity entity)
    {
        return _auditedValueSelector(entity);
    }
}
