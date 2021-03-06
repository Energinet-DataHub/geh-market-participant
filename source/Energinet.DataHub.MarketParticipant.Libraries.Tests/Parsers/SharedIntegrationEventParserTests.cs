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
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.GridArea;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Organization;
using Xunit;
using Xunit.Categories;
using ActorStatus = Energinet.DataHub.MarketParticipant.Integration.Model.Dtos.ActorStatus;
using EicFunction = Energinet.DataHub.MarketParticipant.Integration.Model.Dtos.EicFunction;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers
{
    [UnitTest]
    public class SharedIntegrationEventParserTests
    {
        [Fact]
        public void ParseCorrectlyWith_ActorUpdatedIntegrationEventParser()
        {
            // arrange
            var input = new ActorUpdatedIntegrationEventParser();
            var findAndParse = new SharedIntegrationEventParser();

            var @event = new ActorUpdatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "0123456789012",
                ActorStatus.Active,
                new[] { BusinessRoleCode.Ddk, BusinessRoleCode.Ddm },
                new[] { new ActorMarketRole(EicFunction.Agent, new[] { new ActorGridArea(Guid.NewGuid(), new[] { "t1" }) }) });

            // act
            var actualBytes = input.Parse(@event);
            var actualEventObject = findAndParse.Parse(actualBytes);

            // assert
            Assert.IsType<ActorUpdatedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_GridAreaUpdatedIntegrationEventParser()
        {
            // Arrange
            var input = new GridAreaUpdatedIntegrationEventParser();
            var findAndParse = new SharedIntegrationEventParser();

            var @event = new GridAreaUpdatedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TestArea",
                "123",
                PriceAreaCode.DK1,
                Guid.NewGuid());

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = findAndParse.Parse(actualBytes);

            // Assert
            Assert.IsType<GridAreaUpdatedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_OrganizationUpdatedIntegrationEventParser()
        {
            // Arrange
            var input = new OrganizationUpdatedIntegrationEventParser();
            var findAndParse = new SharedIntegrationEventParser();

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

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = findAndParse.Parse(actualBytes);

            // Assert
            Assert.IsType<OrganizationUpdatedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_GridAreaCreatedIntegrationEventParser()
        {
            // Arrange
            var input = new GridAreaIntegrationEventParser();
            var findAndParse = new SharedIntegrationEventParser();

            var @event = new GridAreaCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "TestArea",
                "123",
                PriceAreaCode.DK1,
                Guid.NewGuid());

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = findAndParse.Parse(actualBytes);

            // Assert
            Assert.IsType<GridAreaCreatedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_GridAreaNameChangedIntegrationEventParser()
        {
            // Arrange
            var input = new GridAreaNameChangedIntegrationEventParser();
            var findAndParse = new SharedIntegrationEventParser();

            var @event = new GridAreaNameChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "TestArea");

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = findAndParse.Parse(actualBytes);

            // Assert
            Assert.IsType<GridAreaNameChangedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_OrganizationCreatedIntegrationEventParser()
        {
            // Arrange
            var input = new OrganizationCreatedIntegrationEventParser();
            var findAndParse = new SharedIntegrationEventParser();

            var @event = new OrganizationCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "TestOrg",
                "12345678",
                new Address(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"));

            @event.Comment = "fake_comment";

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = findAndParse.Parse(actualBytes);

            // Assert
            Assert.IsType<OrganizationCreatedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_OrganizationNameChangedIntegrationEventParser()
        {
            // Arrange
            var input = new OrganizationNameChangedIntegrationEventParser();
            var sharedIntegrationParser = new SharedIntegrationEventParser();

            var @event = new OrganizationNameChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "TestOrg");

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = sharedIntegrationParser.Parse(actualBytes);

            // Assert
            Assert.IsType<OrganizationNameChangedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_OrganizationCommentChangedIntegrationEventParser()
        {
            // Arrange
            var input = new OrganizationCommentChangedIntegrationEventParser();
            var sharedIntegrationParser = new SharedIntegrationEventParser();

            var @event = new OrganizationCommentChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "TestComment");

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = sharedIntegrationParser.Parse(actualBytes);

            // Assert
            Assert.IsType<OrganizationCommentChangedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser()
        {
            // Arrange
            var input = new OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser();
            var sharedIntegrationParser = new SharedIntegrationEventParser();

            var @event = new OrganizationBusinessRegisterIdentifierChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                "BusinessIdentifier");

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = sharedIntegrationParser.Parse(actualBytes);

            // Assert
            Assert.IsType<OrganizationBusinessRegisterIdentifierChangedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseCorrectlyWith_OrganizationAddressChangedIntegrationEventParser()
        {
            // Arrange
            var input = new OrganizationAddressChangedIntegrationEventParser();
            var sharedIntegrationParser = new SharedIntegrationEventParser();

            var @event = new OrganizationAddressChangedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                new Address(
                    "fake_street",
                    "fake_number",
                    "fake_zip",
                    "fake_city",
                    "fake_country"));

            // Act
            var actualBytes = input.Parse(@event);
            var actualEventObject = sharedIntegrationParser.Parse(actualBytes);

            // Assert
            Assert.IsType<OrganizationAddressChangedIntegrationEvent>(actualEventObject);
        }

        [Fact]
        public void ParseException_FallThrough()
        {
            // Arrange
            var findAndParse = new SharedIntegrationEventParser();

            var actualBytes = System.Text.Encoding.ASCII.GetBytes("unknown");

            // Act + Assert
            Assert.Throws<MarketParticipantException>(() => findAndParse.Parse(actualBytes));
        }
    }
}
