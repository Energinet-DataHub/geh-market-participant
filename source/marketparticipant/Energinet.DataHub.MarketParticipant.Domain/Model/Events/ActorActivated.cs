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
using System.ComponentModel;
using System.Text.Json.Serialization;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class ActorActivated : DomainEvent, IIntegrationEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public ActorActivated(
        Guid eventId,
        ActorNumber actorNumber,
        ExternalActorId externalActorId,
        Instant validFrom)
    {
        EventId = eventId;
        ActorNumber = actorNumber;
        ExternalActorId = externalActorId;
        ValidFrom = validFrom;
    }

    public ActorActivated(ActorNumber actorNumber, ExternalActorId externalActorId)
    {
        EventId = Guid.NewGuid();
        ActorNumber = actorNumber;
        ExternalActorId = externalActorId;
        ValidFrom = SystemClock.Instance.GetCurrentInstant();
    }

    public Guid EventId { get; }
    public ActorNumber ActorNumber { get; }
    public ExternalActorId ExternalActorId { get; }
    public Instant ValidFrom { get; }
}
