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
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class ActorConsolidationScheduled : NotificationEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public ActorConsolidationScheduled(
        Guid eventId,
        ActorId recipient,
        ActorNumber affectedActor,
        Instant consolidateAt)
        : base(recipient)
    {
        EventId = eventId;
        AffectedActor = affectedActor;
        ConsolidateAt = consolidateAt;
    }

    public ActorConsolidationScheduled(
        ActorId recipient,
        ActorNumber affectedActor,
        Instant consolidateAt)
        : base(recipient)
    {
        EventId = Guid.NewGuid();
        AffectedActor = affectedActor;
        ConsolidateAt = consolidateAt;
    }

    public ActorNumber AffectedActor { get; }
    public Instant ConsolidateAt { get; }
}
