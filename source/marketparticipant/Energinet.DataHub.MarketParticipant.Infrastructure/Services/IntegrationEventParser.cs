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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public class IntegrationEventParser : IIntegrationEventParser
{
    public DomainEvent? Parse(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        switch (integrationEvent.Message)
        {
            case Model.Contracts.BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged:
                return new BalanceResponsiblePartiesChanged(
                    integrationEvent.EventIdentification,
                    ActorNumber.Create(balanceResponsiblePartiesChanged.EnergySupplierId),
                    ActorNumber.Create(balanceResponsiblePartiesChanged.BalanceResponsibleId),
                    new GridAreaCode(balanceResponsiblePartiesChanged.GridAreaCode),
                    balanceResponsiblePartiesChanged.Received.ToInstant(),
                    balanceResponsiblePartiesChanged.ValidFrom.ToInstant(),
                    balanceResponsiblePartiesChanged.ValidTo.ToInstant());
            default:
                return null;
        }
    }
}
