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

using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed class ActorConsolidation
{
    public ActorConsolidation(ActorId fromId, ActorId toId, Instant scheduledAt)
    {
        Id = new ActorConsolidationId(default);
        ActorFromId = fromId;
        ActorToId = toId;
        ScheduledAt = scheduledAt;
        Status = ActorConsolidationStatus.Pending;
    }

    public ActorConsolidation(ActorConsolidationId id, ActorId fromId, ActorId toId, Instant scheduledAt, ActorConsolidationStatus status)
    {
        Id = id;
        ActorFromId = fromId;
        ActorToId = toId;
        ScheduledAt = scheduledAt;
        Status = status;
    }

    public ActorConsolidationId Id { get; init; }
    public ActorId ActorFromId { get; init; }
    public ActorId ActorToId { get; init; }
    public Instant ScheduledAt { get; set; }
    public ActorConsolidationStatus Status { get; set; }
}