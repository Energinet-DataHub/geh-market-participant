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
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;

public sealed class RevisionLogEntryDto
{
    public RevisionLogEntryDto(
        Guid logId,
        Guid userId,
        Guid actorId,
        Guid systemId,
        Instant occurredOn,
        string activity,
        string origin,
        string payload,
        string affectedEntityType,
        string affectedEntityKey)
    {
        LogId = logId;
        UserId = userId;
        ActorId = actorId;
        SystemId = systemId;
        OccurredOn = occurredOn;
        Activity = activity;
        Origin = origin;
        Payload = payload;
        AffectedEntityType = affectedEntityType;
        AffectedEntityKey = affectedEntityKey;
    }

    public Guid LogId { get; }

    public Guid UserId { get; }
    public Guid ActorId { get; }
    public Guid SystemId { get; }

    public Instant OccurredOn { get; }
    public string Activity { get; }
    public string Origin { get; }
    public string Payload { get; }

    public string AffectedEntityType { get; }
    public string AffectedEntityKey { get; }
}
