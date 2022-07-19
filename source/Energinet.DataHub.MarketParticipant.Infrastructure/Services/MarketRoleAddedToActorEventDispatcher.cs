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
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor;
using MarketRoleAddedToActorIntegrationEvent = Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.ActorIntegrationEvents.MarketRoleAddedToActorIntegrationEvent;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class MarketRoleAddedToActorEventDispatcher : EventDispatcherBase
{
    private readonly IMarketRoleAddedToActorIntegrationEventParser _eventParser;
    private readonly IMarketParticipantServiceBusClient _serviceBusClient;

    public MarketRoleAddedToActorEventDispatcher(
        IMarketRoleAddedToActorIntegrationEventParser eventParser,
        IMarketParticipantServiceBusClient serviceBusClient)
    {
        _eventParser = eventParser;
        _serviceBusClient = serviceBusClient;
    }

    public override async Task<bool> TryDispatchAsync(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (integrationEvent is not MarketRoleAddedToActorIntegrationEvent marketRoleAddedToActorIntegrationEvent)
        {
            return false;
        }

        var outboundIntegrationEvent = new Integration.Model.Dtos.MarketRoleAddedToActorIntegrationEvent(
            marketRoleAddedToActorIntegrationEvent.Id,
            marketRoleAddedToActorIntegrationEvent.ActorId,
            marketRoleAddedToActorIntegrationEvent.OrganizationId.Value,
            (BusinessRoleCode)marketRoleAddedToActorIntegrationEvent.BusinessRole,
            (EicFunction)marketRoleAddedToActorIntegrationEvent.MarketRole,
            marketRoleAddedToActorIntegrationEvent.EventCreated);

        var bytes = _eventParser.Parse(outboundIntegrationEvent);
        var message = new ServiceBusMessage(bytes);
        SetMessageMetaData(message, outboundIntegrationEvent);

        var sender = _serviceBusClient.CreateSender();

        await using (sender.ConfigureAwait(false))
        {
            await sender.SendMessageAsync(message).ConfigureAwait(false);
        }

        return true;
    }
}
