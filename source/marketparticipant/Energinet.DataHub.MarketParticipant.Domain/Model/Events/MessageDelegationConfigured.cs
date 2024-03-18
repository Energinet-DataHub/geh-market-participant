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
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class MessageDelegationConfigured : DomainEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public MessageDelegationConfigured(
        Guid eventId,
        ActorId delegatedBy,
        ActorId delegatedTo,
        DelegationMessageType messageType,
        GridAreaId gridAreaId,
        Instant startsAt,
        Instant stopsAt)
    {
        EventId = eventId;
        DelegatedBy = delegatedBy;
        DelegatedTo = delegatedTo;
        MessageType = messageType;
        GridAreaId = gridAreaId;
        StartsAt = startsAt;
        StopsAt = stopsAt;
    }

    public MessageDelegationConfigured(
        ActorId delegatedBy,
        ActorId delegatedTo,
        DelegationMessageType messageType,
        GridAreaId gridAreaId,
        Instant startsAt)
    {
        EventId = Guid.NewGuid();
        DelegatedBy = delegatedBy;
        DelegatedTo = delegatedTo;
        MessageType = messageType;
        GridAreaId = gridAreaId;
        StartsAt = startsAt;
        StopsAt = Instant.MaxValue;
    }

    public MessageDelegationConfigured(
        ActorId delegatedBy,
        ActorId delegatedTo,
        DelegationMessageType messageType,
        GridAreaId gridAreaId,
        Instant startsAt,
        Instant stopsAt)
    {
        EventId = Guid.NewGuid();
        DelegatedBy = delegatedBy;
        DelegatedTo = delegatedTo;
        MessageType = messageType;
        GridAreaId = gridAreaId;
        StartsAt = startsAt;
        StopsAt = stopsAt;
    }

    public ActorId DelegatedBy { get; }
    public ActorId DelegatedTo { get; }
    public DelegationMessageType MessageType { get; }
    public GridAreaId GridAreaId { get; }
    public Instant StartsAt { get; }
    public Instant StopsAt { get; }
}
