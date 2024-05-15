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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.MarketParticipant.Application.Contracts;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Integration;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Integration;

public class IntegrationEventSubscriptionHandler(
    IBalanceResponsiblePartiesChangedEventHandler balanceResponsiblePartiesChangedEventHandler,
    ILogger<IntegrationEventSubscriptionHandler> logger) : IIntegrationEventHandler
{
    public Task HandleAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        switch (integrationEvent.Message)
        {
            case BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged:
                logger.LogInformation("Received BalanceResponsiblePartiesChanged event with id {EventId}", integrationEvent.EventIdentification);
                return balanceResponsiblePartiesChangedEventHandler.HandleAsync(balanceResponsiblePartiesChanged);
            default:
                return Task.CompletedTask;
        }
    }
}
