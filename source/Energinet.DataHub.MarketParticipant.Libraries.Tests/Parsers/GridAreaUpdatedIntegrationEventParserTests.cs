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
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers
{
    [UnitTest]
    public class GridAreaUpdatedIntegrationEventParserTests
    {
        [Fact]
        public void Parse_InputValid_ParsesCorrectly()
        {
            // arrange
            var target = new GridAreaUpdatedIntegrationEventParser();
            var @event = new GridAreaUpdatedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TestArea",
                "123",
                PriceAreaCode.DK1,
                Guid.NewGuid());

            // act
            var actualBytes = target.Parse(@event);
            var actualEvent = target.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.Code, actualEvent.Code);
            Assert.Equal(@event.Name, actualEvent.Name);
            Assert.Equal(@event.GridAreaId, actualEvent.GridAreaId);
            Assert.Equal(@event.PriceAreaCode, actualEvent.PriceAreaCode);
            Assert.Equal(@event.GridAreaLinkId, actualEvent.GridAreaLinkId);
        }

        [Fact]
        public void Parse_InvalidGuid_ThrowsException()
        {
            // Arrange
            var target = new GridAreaUpdatedIntegrationEventParser();
            var contract = new GridAreaUpdatedIntegrationEventContract
            {
                Id = "Not_A_Guid",
                Name = "fake_value",
                Code = "123",
                GridAreaId = Guid.NewGuid().ToString(),
                PriceAreaCode = (int)PriceAreaCode.DK1,
                GridAreaLinkId = Guid.NewGuid().ToString()
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvaliGridAreaGuid_ThrowsException()
        {
            // Arrange
            var target = new GridAreaUpdatedIntegrationEventParser();
            var contract = new GridAreaUpdatedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                Name = "fake_value",
                Code = "123",
                GridAreaId = "Not_A_Guid",
                PriceAreaCode = (int)PriceAreaCode.DK1,
                GridAreaLinkId = Guid.NewGuid().ToString()
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvaliGridAreaLinkGuid_ThrowsException()
        {
            // Arrange
            var target = new GridAreaUpdatedIntegrationEventParser();
            var contract = new GridAreaUpdatedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                Name = "fake_value",
                Code = "123",
                GridAreaId = Guid.NewGuid().ToString(),
                PriceAreaCode = (int)PriceAreaCode.DK1,
                GridAreaLinkId = "Not_A_Giud"
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidEnum_ThrowsException()
        {
            // Arrange
            var target = new GridAreaUpdatedIntegrationEventParser();
            var contract = new GridAreaUpdatedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                Name = "fake_value",
                Code = "123",
                GridAreaId = Guid.NewGuid().ToString(),
                PriceAreaCode = 34
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidInput_ThrowsException()
        {
            // Arrange
            var target = new GridAreaUpdatedIntegrationEventParser();

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => target.Parse(new byte[] { 1, 2, 3 }));
        }
    }
}
