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
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;

public sealed class DispatchIntegrationEventsTrigger
{
    private readonly IPublisher _publisher;

    public DispatchIntegrationEventsTrigger(IPublisher publisher)
    {
        _publisher = publisher;
    }

    [Function(nameof(DispatchIntegrationEventsTrigger))]
    public Task RunAsync([TimerTrigger("* */1 * * *")] FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _publisher.PublishAsync(context.CancellationToken);
    }
}
