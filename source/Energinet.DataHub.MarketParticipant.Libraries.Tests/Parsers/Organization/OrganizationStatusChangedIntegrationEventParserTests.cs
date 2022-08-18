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
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.GridArea;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Organization;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers.Organization
{
    [UnitTest]
    public class OrganizationStatusChangedIntegrationEventParserTests
    {
        [Fact]
        public void Parse_InputValid_ParsesCorrectly()
        {
            // arrange
            var target = new OrganizationStatusChangedIntegrationEventParser();
            var targetGrid = new OrganizationStatusChangedIntegrationEventParser();

            var @event = new OrganizationStatusChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                OrganizationStatus.Active);

            // act
            var actualBytes = targetGrid.Parse(@event);
            var actualEvent = OrganizationStatusChangedIntegrationEventParser.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.OrganizationId, actualEvent.OrganizationId);
            Assert.Equal(@event.Status, actualEvent.Status);
        }

        [Fact]
        public void Parse_InvalidStatus_ThrowsException()
        {
            // Arrange
            var target = new OrganizationStatusChangedIntegrationEventParser();
            var contract = new OrganizationStatusChangedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                Status = 0,
                OrganizationId = Guid.NewGuid().ToString(),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => OrganizationStatusChangedIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidInput_ThrowsException()
        {
            // Arrange
            var target = new OrganizationStatusChangedIntegrationEventParser();

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => OrganizationStatusChangedIntegrationEventParser.Parse(new byte[] { 1, 2, 3 }));
        }
    }
}
