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

public sealed class GridAreaOwnershipAssigned : DomainEvent, IIntegrationEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public GridAreaOwnershipAssigned(
        Guid eventId,
        ActorNumber actorNumber,
        EicFunction actorRole,
        GridAreaId gridAreaId,
        Instant validFrom)
    {
        EventId = eventId;
        ActorNumber = actorNumber;
        ActorRole = actorRole;
        GridAreaId = gridAreaId;
        ValidFrom = validFrom;
    }

    public GridAreaOwnershipAssigned(ActorNumber actorNumber, EicFunction actorRole, GridAreaId gridAreaId)
    {
        EventId = Guid.NewGuid();
        ActorNumber = actorNumber;
        ActorRole = actorRole;
        GridAreaId = gridAreaId;

        var currentInstant = Clock.Instance.GetCurrentInstant();

        var localDate = currentInstant.InZone(Clock.TimeZoneDk).Date;
        var nextDate = localDate.PlusDays(1);

        ValidFrom = nextDate.AtStartOfDayInZone(Clock.TimeZoneDk).ToInstant();
    }

    public Guid EventId { get; }
    public ActorNumber ActorNumber { get; }
    public EicFunction ActorRole { get; }
    public GridAreaId GridAreaId { get; }
    public Instant ValidFrom { get; }
}
