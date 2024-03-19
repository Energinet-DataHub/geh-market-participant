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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.MarketParticipant.Application.Contracts;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.Organization;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ReceiveIntegrationEventsIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task ReceivesBalanceResponsibleChangeEvent_ValidToWithValue_EmailEventCreated()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(fixture);
        await fixture.EmailEventsClearNotSentAsync();

        await using var scope = host.BeginScope();
        var subscriber = scope.ServiceProvider.GetRequiredService<ISubscriber>();

        var contractToParse = GetBalanceResponsiblePartiesChange(false);

        // Act
        var eventMessage = CreateEvent(contractToParse);
        await subscriber.HandleAsync(eventMessage);

        //Assert
        var context1 = fixture.DatabaseManager.CreateDbContext();
        var emailEvent = context1.EmailEventEntries.First(e => e.Sent == null && e.TemplateId == (int)EmailTemplateId.BalanceResponsiblePartiesChanged);
        var templateParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(emailEvent.TemplateParameters);

        Assert.NotNull(templateParameters);
        Assert.Equal(contractToParse.BalanceResponsibleId, templateParameters["actor_balance_responsible"]);
        Assert.Equal(contractToParse.EnergySupplierId, templateParameters["actor_supplier"]);
        Assert.Equal(contractToParse.GridAreaCode, templateParameters["grid_area_code"]);
        Assert.Equal(contractToParse.ValidFrom.ToDateTimeOffset().ToString("u"), templateParameters["valid_from"]);
        Assert.Equal(contractToParse.ValidTo.ToDateTimeOffset().ToString("u"), templateParameters["valid_to"]);
    }

    [Fact]
    public async Task ReceivesBalanceResponsibleChangeEvent_ValidToWithoutValue_EmailEventCreated()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(fixture);
        await fixture.EmailEventsClearNotSentAsync();

        await using var scope = host.BeginScope();
        var subscriber = scope.ServiceProvider.GetRequiredService<ISubscriber>();

        var contractToParse = GetBalanceResponsiblePartiesChange(true);

        // Act
        var eventMessage = CreateEvent(contractToParse);
        await subscriber.HandleAsync(eventMessage);

        //Assert
        var context1 = fixture.DatabaseManager.CreateDbContext();
        var emailEvent = context1.EmailEventEntries.First(e => e.Sent == null && e.TemplateId == (int)EmailTemplateId.BalanceResponsiblePartiesChanged);
        var templateParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(emailEvent.TemplateParameters);

        Assert.NotNull(templateParameters);
        Assert.Equal(contractToParse.BalanceResponsibleId, templateParameters["actor_balance_responsible"]);
        Assert.Equal(contractToParse.EnergySupplierId, templateParameters["actor_supplier"]);
        Assert.Equal(contractToParse.GridAreaCode, templateParameters["grid_area_code"]);
        Assert.Equal(contractToParse.ValidFrom.ToDateTimeOffset().ToString("u"), templateParameters["valid_from"]);
        Assert.Equal(string.Empty, templateParameters["valid_to"]);
        Assert.Null(contractToParse.ValidTo);
    }

    private static IntegrationEventServiceBusMessage CreateEvent(IMessage contractToParse)
    {
        var messageInBytes = contractToParse.ToByteArray();

        var bindingData = new Dictionary<string, object>
        {
            { "MessageId", Guid.NewGuid().ToString() },
            { "Subject", "BalanceResponsiblePartiesChanged" },
            {
                "ApplicationProperties", JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "EventName", "BalanceResponsiblePartiesChanged" },
                    { "EventIdentification", Guid.NewGuid().ToString() },
                    { "EventMinorVersion", 1 },
                    { "CorrelationId", "123" }
                })
            }
        };

        var eventMessage = IntegrationEventServiceBusMessage.Create(messageInBytes, bindingData);
        return eventMessage;
    }

    private static BalanceResponsiblePartiesChanged GetBalanceResponsiblePartiesChange(bool validToIsNull)
    {
        var b = new BalanceResponsiblePartiesChanged
        {
            BalanceResponsibleId = new MockedGln(),
            EnergySupplierId = new MockedGln(),
            ValidFrom = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            ValidTo = validToIsNull ? null : Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            Received = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            GridAreaCode = "123",
            MeteringPointType = MeteringPointType.Consumption
        };
        return b;
    }
}
