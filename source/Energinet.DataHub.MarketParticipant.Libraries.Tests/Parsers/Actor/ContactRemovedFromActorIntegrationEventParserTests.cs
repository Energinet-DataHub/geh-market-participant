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
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers.Actor
{
    [UnitTest]
    public class ContactRemovedFromActorIntegrationEventParserTests
    {
        [Theory]
        [InlineData("34343434")]
        [InlineData(null)]
        public void Parse_InputValid_ParsesCorrectly(string? phone)
        {
            // arrange
            var target = new ContactRemovedFromActorIntegrationEventParser();
            var @event = new ContactRemovedFromActorIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                new ActorContact(
                    "fake_name",
                    "fake_email@me.dk",
                    ContactCategory.Default,
                    phone));

            // act
            var actualBytes = target.Parse(@event);
            var actualEvent = ContactRemovedFromActorIntegrationEventParser.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.EventCreated, actualEvent.EventCreated);
            Assert.Equal(@event.ActorId, actualEvent.ActorId);
            Assert.Equal(@event.OrganizationId, actualEvent.OrganizationId);
            Assert.Equal(@event.Type, actualEvent.Type);
            Assert.Equal(@event.Contact.Name, actualEvent.Contact.Name);
            Assert.Equal(@event.Contact.Email, actualEvent.Contact.Email);
            Assert.Equal(@event.Contact.Phone, actualEvent.Contact.Phone);
            Assert.Equal(@event.Contact.Category, actualEvent.Contact.Category);
        }

        [Fact]
        public void Parse_InvalidGuid_ThrowsException()
        {
            // Arrange
            var target = new ContactRemovedFromActorIntegrationEventParser();
            var contract = new ContactRemovedFromActorIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                ActorId = "not_a_guid",
                OrganizationId = Guid.NewGuid().ToString(),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
                Type = nameof(GridAreaAddedToActorIntegrationEvent),
                Contact = new ActorContactEventData
                {
                    Name = "fake_name",
                    Email = "fake_email",
                    Category = 0,
                    Phone = "34343434"
                }
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ContactRemovedFromActorIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidContactCategory_ThrowsException()
        {
            // Arrange
            var target = new ContactRemovedFromActorIntegrationEventParser();
            var contract = new ContactRemovedFromActorIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                ActorId = Guid.NewGuid().ToString(),
                OrganizationId = Guid.NewGuid().ToString(),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
                Type = nameof(GridAreaAddedToActorIntegrationEvent),
                Contact = new ActorContactEventData
                {
                    Name = "fake_name",
                    Email = "fake_email",
                    Category = -1,
                    Phone = "34343434"
                }
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ContactRemovedFromActorIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidMissingContact_ThrowsException()
        {
            // Arrange
            var target = new ContactRemovedFromActorIntegrationEventParser();
            var contract = new ContactRemovedFromActorIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                ActorId = Guid.NewGuid().ToString(),
                OrganizationId = Guid.NewGuid().ToString(),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
                Type = nameof(GridAreaAddedToActorIntegrationEvent)
            };

            // Act + Assert
            Assert.Throws<NullReferenceException>(() => ContactRemovedFromActorIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidInput_ThrowsException()
        {
            // Arrange
            var target = new ContactRemovedFromActorIntegrationEventParser();

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ContactRemovedFromActorIntegrationEventParser.Parse(new byte[] { 1, 2, 3 }));
        }
    }
}
