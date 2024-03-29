﻿// Copyright 2020 Energinet DataHub A/S
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
using System.ComponentModel;
using System.Text.Json.Serialization;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class ProcessDelegationConfigured : DomainEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public ProcessDelegationConfigured(
        Guid eventId,
        ActorId delegatedBy,
        ActorId delegatedTo,
        DelegatedProcess process,
        GridAreaId gridAreaId,
        Instant startsAt,
        Instant stopsAt)
    {
        EventId = eventId;
        DelegatedBy = delegatedBy;
        DelegatedTo = delegatedTo;
        Process = process;
        GridAreaId = gridAreaId;
        StartsAt = startsAt;
        StopsAt = stopsAt;
    }

    public ProcessDelegationConfigured(ProcessDelegation processDelegation, DelegationPeriod delegationPeriod)
    {
        ArgumentNullException.ThrowIfNull(processDelegation);
        ArgumentNullException.ThrowIfNull(delegationPeriod);

        EventId = Guid.NewGuid();
        DelegatedBy = processDelegation.DelegatedBy;
        DelegatedTo = delegationPeriod.DelegatedTo;
        Process = processDelegation.Process;
        GridAreaId = delegationPeriod.GridAreaId;
        StartsAt = delegationPeriod.StartsAt;
        StopsAt = delegationPeriod.StopsAt ?? Instant.MaxValue;
    }

    public ActorId DelegatedBy { get; }
    public ActorId DelegatedTo { get; }
    public DelegatedProcess Process { get; }
    public GridAreaId GridAreaId { get; }
    public Instant StartsAt { get; }
    public Instant StopsAt { get; }
}
