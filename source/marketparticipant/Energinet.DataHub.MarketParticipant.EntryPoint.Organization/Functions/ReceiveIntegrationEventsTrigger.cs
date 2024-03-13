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
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;

public sealed class ReceiveIntegrationEventsTrigger(ISubscriber subscriber)
{
    [Function(nameof(ReceiveIntegrationEventsTrigger))]
    public Task RunAsync(
        [ServiceBusTrigger(
            $"%{nameof(ConsumeServiceBusSettings)}:{nameof(ConsumeServiceBusSettings.SharedIntegrationEventTopic)}%",
            $"%{nameof(ConsumeServiceBusSettings)}:{nameof(ConsumeServiceBusSettings.IntegrationEventSubscription)}%",
            Connection = $"{nameof(ConsumeServiceBusSettings)}:{nameof(ConsumeServiceBusSettings.ConnectionString)}")]
        byte[] message,
        FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return subscriber.HandleAsync(IntegrationEventServiceBusMessage.Create(message, context.BindingContext.BindingData!));
    }
}
