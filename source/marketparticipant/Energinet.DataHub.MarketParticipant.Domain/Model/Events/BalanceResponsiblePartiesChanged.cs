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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class BalanceResponsiblePartiesChanged : DomainEvent
{
    public BalanceResponsiblePartiesChanged(
        Guid eventId,
        ActorNumber electricalSupplier,
        ActorNumber balanceResponsibleParty,
        GridAreaCode gridAreaCode,
        Instant received,
        Instant validFrom,
        Instant? validTo)
    {
        EventId = eventId;
        ElectricalSupplier = electricalSupplier;
        BalanceResponsibleParty = balanceResponsibleParty;
        GridAreaCode = gridAreaCode;
        Received = received;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public ActorNumber ElectricalSupplier { get; }
    public ActorNumber BalanceResponsibleParty { get; }
    public GridAreaCode GridAreaCode { get; }
    public Instant Received { get; }
    public Instant ValidFrom { get; }
    public Instant? ValidTo { get; }
}
