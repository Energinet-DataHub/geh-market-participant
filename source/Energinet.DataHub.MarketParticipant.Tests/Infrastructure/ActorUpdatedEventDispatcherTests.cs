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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Infrastructure
{
    [UnitTest]
    public sealed class ActorUpdatedEventDispatcherTests
    {
        [Fact]
        public async Task ActorUpdated_IntegrationEventDispatcher_CanReadEvent()
        {
            // arrange
            await using var serviceBusSenderMock = new MockedServiceBusSender();
            var serviceBusClient = new Mock<IMarketParticipantServiceBusClient>();
            serviceBusClient.Setup(x => x.CreateSender()).Returns(serviceBusSenderMock);
            var actorEventParser = new ActorUpdatedIntegrationEventParser();
            var eventParser = new SharedIntegrationEventParser();
            var target = new ActorUpdatedEventDispatcher(actorEventParser, serviceBusClient.Object);

            var integrationEvent = new ActorUpdatedIntegrationEvent
            {
                ActorId = Guid.NewGuid(),
                OrganizationId = new OrganizationId(Guid.NewGuid()),
                ExternalActorId = new ExternalActorId(Guid.NewGuid()),
                Gln = new GlobalLocationNumber("gln"),
                Status = ActorStatus.Active
            };
            integrationEvent.BusinessRoles.Add(BusinessRoleCode.Ddk);
            integrationEvent.MarketRoles.Add(EicFunction.BalancingServiceProvider);
            integrationEvent.GridAreas.Add(new GridAreaId(Guid.NewGuid()));

            // act
            var actual = await target.TryDispatchAsync(integrationEvent).ConfigureAwait(false);
            var actualMessage = serviceBusSenderMock.SentMessages.Single();
            var actualEvent = eventParser.Parse(actualMessage.Body.ToArray()) as MarketParticipant.Integration.Model.Dtos.ActorUpdatedIntegrationEvent;

            // assert
            Assert.True(actual);
            Assert.NotNull(actualEvent);
            Assert.Equal(integrationEvent.Id, actualEvent!.Id);
            Assert.Equal(integrationEvent.OrganizationId.Value, actualEvent.OrganizationId);
            Assert.Equal(integrationEvent.ExternalActorId.Value, actualEvent.ExternalActorId);
            Assert.Equal(integrationEvent.Gln.Value, actualEvent.Gln);
            Assert.Equal((int)integrationEvent.Status, (int)actualEvent.Status);
            Assert.Equal((int)integrationEvent.BusinessRoles.Single(), (int)actualEvent.BusinessRoles.Single());
            Assert.Equal((int)integrationEvent.MarketRoles.Single(), (int)actualEvent.MarketRoles.Single());
            Assert.Equal(integrationEvent.GridAreas.Single().Value.ToString(), actualEvent.GridAreas.Single().ToString());
        }

        [Fact]
        public async Task ActorUpdatedIntegrationEventDispatcher_WrongEventType_ReturnsFalse()
        {
            // arrange
            await using var serviceBusSenderMock = new MockedServiceBusSender();
            var serviceBusClient = new Mock<IMarketParticipantServiceBusClient>();
            serviceBusClient.Setup(x => x.CreateSender()).Returns(serviceBusSenderMock);

            var eventParser = new ActorUpdatedIntegrationEventParser();
            var target = new ActorUpdatedEventDispatcher(eventParser, serviceBusClient.Object);

            var integrationEvent = new OrganizationUpdatedIntegrationEvent
            {
                Address = new Address(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"),
                Name = "fake_value",
                OrganizationId = new OrganizationId(Guid.NewGuid()),
                BusinessRegisterIdentifier = new BusinessRegisterIdentifier("12345678")
            };

            // act
            var actual = await target.TryDispatchAsync(integrationEvent).ConfigureAwait(false);

            // assert
            Assert.False(actual);
        }
    }
}
