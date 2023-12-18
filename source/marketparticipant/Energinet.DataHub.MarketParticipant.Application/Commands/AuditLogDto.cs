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
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Application.Commands;

public sealed record AuditLogDto<TAuditedChange>
{
    public AuditLogDto(AuditLog<TAuditedChange> auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        Change = auditLog.Change;
        Timestamp = auditLog.Timestamp.ToDateTimeOffset();
        AuditIdentityId = auditLog.AuditIdentity.Value;
        IsInitialAssignment = auditLog.IsInitialAssignment;
        CurrentValue = auditLog.CurrentValue;
        PreviousValue = auditLog.PreviousValue;
    }

    public TAuditedChange Change { get; }
    public DateTimeOffset Timestamp { get; }
    public Guid AuditIdentityId { get; }
    public bool IsInitialAssignment { get; }
    public string? CurrentValue { get; }
    public string? PreviousValue { get; }
}
