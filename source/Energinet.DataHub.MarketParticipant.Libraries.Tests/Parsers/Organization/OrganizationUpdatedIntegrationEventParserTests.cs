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
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers.Organization
{
    [UnitTest]
    public class OrganizationUpdatedIntegrationEventParserTests
    {
        [Fact]
        public void Parse_InputValid_ParsesCorrectly()
        {
            // arrange
            var target = new OrganizationUpdatedIntegrationEventParser();
            var @event = new OrganizationUpdatedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TestOrg",
                "12345678",
                new Address(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"));

            // act
            var actualBytes = target.Parse(@event);
            var actualEvent = target.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.OrganizationId, actualEvent.OrganizationId);
            Assert.Equal(@event.Name, actualEvent.Name);
            Assert.Equal(@event.BusinessRegisterIdentifier, actualEvent.BusinessRegisterIdentifier);
            Assert.Equal(@event.Address.City, actualEvent.Address.City);
            Assert.Equal(@event.Address.Country, actualEvent.Address.Country);
            Assert.Equal(@event.Address.Number, actualEvent.Address.Number);
            Assert.Equal(@event.Address.StreetName, actualEvent.Address.StreetName);
            Assert.Equal(@event.Address.ZipCode, actualEvent.Address.ZipCode);
        }

        [Fact]
        public void Parse_InvalidGuid_ThrowsException()
        {
            // Arrange
            var target = new OrganizationUpdatedIntegrationEventParser();
            var contract = new OrganizationUpdatedIntegrationEventContract
            {
                Address = new OrganizationAddressEventData()
                {
                    City = "fake_value",
                    Country = "fake_value",
                    Number = "fake_value",
                    StreetName = "fake_value",
                    ZipCode = "fake_value"
                },
                Id = "Not_A_Giud",
                BusinessRegisterIdentifier = "12345678",
                Name = "fake_value",
                OrganizationId = Guid.NewGuid().ToString()
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidInput_ThrowsException()
        {
            // Arrange
            var target = new OrganizationUpdatedIntegrationEventParser();

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(new byte[] { 1, 2, 3 }));
        }
    }
}
