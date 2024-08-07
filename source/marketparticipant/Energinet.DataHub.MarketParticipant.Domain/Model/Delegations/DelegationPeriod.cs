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
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

public sealed record DelegationPeriod(ActorId DelegatedTo, GridAreaId GridAreaId, Instant StartsAt)
{
    public DelegationPeriod(
        DelegationPeriodId id,
        ActorId delegatedTo,
        GridAreaId gridAreaId,
        Instant startsAt,
        Instant? stopsAt)
            : this(delegatedTo, gridAreaId, startsAt)
    {
        Id = id;
        StopsAt = stopsAt;
    }

    public DelegationPeriodId Id { get; } = new(Guid.Empty);
    public Instant? StopsAt { get; init; }

    public bool IsCancelled => StopsAt <= StartsAt;
}
