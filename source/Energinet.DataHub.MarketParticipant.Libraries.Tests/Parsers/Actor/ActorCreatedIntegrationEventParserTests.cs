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
    public class ActorCreatedIntegrationEventParserTests
    {
        [Fact]
        public void Parse_InputValid_ParsesCorrectly()
        {
            // arrange
            var target = new ActorCreatedIntegrationEventParser();
            var @event = new ActorCreatedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                ActorStatus.Active,
                "0123456789012",
                "ActorName",
                new[] { BusinessRoleCode.Ddk, BusinessRoleCode.Ddm },
                new[] { new Integration.Model.Dtos.ActorMarketRole(EicFunction.Agent, new[] { new Integration.Model.Dtos.ActorGridArea(Guid.NewGuid(), new[] { "t1" }) }) },
                DateTime.UtcNow);

            // act
            var actualBytes = target.Parse(@event);
            var actualEvent = ActorCreatedIntegrationEventParser.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.ActorId, actualEvent.ActorId);
            Assert.Equal(@event.OrganizationId, actualEvent.OrganizationId);
            Assert.Equal(@event.ActorNumber, actualEvent.ActorNumber);
            Assert.Equal(@event.Name, actualEvent.Name);
            Assert.Equal(@event.Status, actualEvent.Status);
            Assert.Equal(@event.BusinessRoles.Count(), actualEvent.BusinessRoles.Count());
            Assert.Equal(@event.ActorMarketRoles.Count(), actualEvent.ActorMarketRoles.Count());

            var expRoles = @event.ActorMarketRoles.ToList();
            var actRoles = actualEvent.ActorMarketRoles.ToList();

            for (int i = 0; i < expRoles.Count; i++)
            {
                var expGrids = expRoles[i].GridAreas.ToList();
                var actGrids = actRoles[i].GridAreas.ToList();

                for (int j = 0; j < expGrids.Count; j++)
                {
                    var expPoints = expGrids[j].MeteringPointTypes.ToList();
                    var actPoints = actGrids[j].MeteringPointTypes.ToList();

                    for (int k = 0; k < expPoints.Count; k++)
                    {
                        Assert.Equal(expPoints[k], actPoints[k]);
                    }

                    Assert.Equal(expGrids[j].Id, actGrids[j].Id);
                }

                Assert.Equal(expRoles[i].Function, actRoles[i].Function);
            }
        }

        [Fact]
        public void Parse_InvalidGuid_ThrowsException()
        {
            // Arrange
            var target = new ActorCreatedIntegrationEventParser();
            var contract = new ActorCreatedIntegrationEventContract
            {
                Id = "Not_A_Giud",
                ActorNumber = "fake_value",
                Status = 2,
                ActorId = Guid.NewGuid().ToString(),
                BusinessRoles = { 1 },
                OrganizationId = Guid.NewGuid().ToString(),
                Type = nameof(ActorCreatedIntegrationEvent),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
                Name = "ActorName"
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ActorCreatedIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidStatusEnum_ThrowsException()
        {
            // Arrange
            var target = new ActorCreatedIntegrationEventParser();
            var contract = new ActorCreatedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                ActorNumber = Guid.NewGuid().ToString(),
                Status = -1,
                ActorId = Guid.NewGuid().ToString(),
                BusinessRoles = { 1 },
                OrganizationId = Guid.NewGuid().ToString(),
                Type = nameof(ActorCreatedIntegrationEvent),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ActorCreatedIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidBusinessRolesEnum_ThrowsException()
        {
            // Arrange
            var target = new ActorCreatedIntegrationEventParser();
            var contract = new ActorCreatedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                ActorNumber = Guid.NewGuid().ToString(),
                Status = 1,
                ActorId = Guid.NewGuid().ToString(),
                BusinessRoles = { -1, -2 },
                OrganizationId = Guid.NewGuid().ToString(),
                Type = nameof(ActorCreatedIntegrationEvent),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ActorCreatedIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidMarketRolesEnum_ThrowsException()
        {
            // Arrange
            var target = new ActorCreatedIntegrationEventParser();
            var contract = new ActorCreatedIntegrationEventContract
            {
                Id = Guid.NewGuid().ToString(),
                ActorNumber = Guid.NewGuid().ToString(),
                Status = 1,
                ActorId = Guid.NewGuid().ToString(),
                BusinessRoles = { 1 },
                OrganizationId = Guid.NewGuid().ToString(),
                Type = nameof(ActorCreatedIntegrationEvent),
                EventCreated = Timestamp.FromDateTime(DateTime.UtcNow),
                ActorMarketRoles = { new List<ActorMarketRoleEventData>() { new() { Function = -1 } } }
            };

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ActorCreatedIntegrationEventParser.Parse(contract.ToByteArray()));
        }

        [Fact]
        public void Parse_InvalidInput_ThrowsException()
        {
            // Arrange
            var target = new ActorCreatedIntegrationEventParser();

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => ActorCreatedIntegrationEventParser.Parse(new byte[] { 1, 2, 3 }));
        }
    }
}
