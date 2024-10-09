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

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed record BalanceResponsibilityRelation(ActorId EnergySupplier, GridAreaId GridArea, MeteringPointType MeteringPointType, Instant ValidFrom)
{
    public BalanceResponsibilityRelation(
        ActorId energySupplier,
        GridAreaId gridArea,
        MeteringPointType meteringPointType,
        Instant validFrom,
        Instant? validTo,
        Instant? validToAssignedAt)
            : this(energySupplier, gridArea, meteringPointType, validFrom)
    {
        if (validTo.HasValue != validToAssignedAt.HasValue)
        {
            ArgumentNullException.ThrowIfNull(validTo);
            ArgumentNullException.ThrowIfNull(validToAssignedAt);
        }

        ValidTo = validTo;
        ValidToAssignedAt = validToAssignedAt;
    }

    public Instant? ValidTo { get; init; }
    public Instant? ValidToAssignedAt { get; init; }
}
