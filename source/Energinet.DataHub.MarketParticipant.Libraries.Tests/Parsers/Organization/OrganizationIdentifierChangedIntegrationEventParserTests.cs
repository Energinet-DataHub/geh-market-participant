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
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Organization;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers.Organization
{
    [UnitTest]
    public class OrganizationBusinessRegisterIdentifierChangedIntegrationEventParserTests
    {
        [Fact]
        public void Parse_InputValid_ParsesCorrectly()
        {
            // arrange
            var target = new OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser();
            var @event = new OrganizationBusinessRegisterIdentifierChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "identifer");

            // act
            var actualBytes = target.Parse(@event);
            var actualEvent = OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.OrganizationId, actualEvent.OrganizationId);
            Assert.Equal(@event.BusinessRegisterIdentifier, actualEvent.BusinessRegisterIdentifier);
        }

        [Fact]
        public void Parse_InvalidGuid_ThrowsException()
        {
            // Arrange
            var target = new OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser();
            var contract = new OrganizationBusinessRegisterIdentifierChangedIntegrationEventContract
            {
                Id = "Not_A_Giud",
                BusinessRegisterIdentifier = "fake_value",
                OrganizationId = Guid.NewGuid().ToString(),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidInput_ThrowsException()
        {
            // Arrange
            var target = new OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser();

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser.Parse(new byte[] { 1, 2, 3 }));
        }
    }
}
