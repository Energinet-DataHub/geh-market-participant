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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class BalanceResponsibilityValidationFailed : NotificationEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public BalanceResponsibilityValidationFailed(
        Guid eventId,
        ActorId recipient,
        ActorNumber actorNumber,
        bool isActorUnrecognized)
        : base(recipient)
    {
        EventId = eventId;
        ActorNumber = actorNumber;
        IsActorUnrecognized = isActorUnrecognized;
    }

    public BalanceResponsibilityValidationFailed(
        ActorId recipient,
        ActorNumber actorNumber,
        bool isActorUnrecognized)
        : base(recipient)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);

        EventId = Guid.NewGuid();
        ActorNumber = actorNumber;
        IsActorUnrecognized = isActorUnrecognized;
    }

    public ActorNumber ActorNumber { get; }
    public bool IsActorUnrecognized { get; }
}
