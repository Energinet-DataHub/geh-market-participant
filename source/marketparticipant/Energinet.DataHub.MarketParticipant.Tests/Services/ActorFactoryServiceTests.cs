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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Energinet.DataHub.MarketParticipant.Tests.Handlers;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class ActorFactoryServiceTests
    {
        private readonly Address _validAddress = new(
            "test Street",
            "1",
            "1111",
            "Test City",
            "Test Country");

        private readonly BusinessRegisterIdentifier _validCvrBusinessRegisterIdentifier = new("12345678");
        private readonly OrganizationDomain _validDomain = new("energinet.dk");

        [Fact]
        public async Task CreateAsync_NewActor_AddsAndReturnsActor()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();

            var target = new ActorFactoryService(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object);

            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, _validDomain, null);
            var marketRoles = new List<ActorMarketRole> { new(EicFunction.EnergySupplier, Enumerable.Empty<ActorGridArea>()) };

            actorRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
                .ReturnsAsync(new Actor(organization.Id, new MockedGln()));

            // Act
            var response = await target
                .CreateAsync(
                    organization,
                    new MockedGln(),
                    new ActorName("fake_value"),
                    marketRoles)
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task CreateAsync_NewActor_EnsuresGlnUniqueness()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var globalLocationNumberUniquenessService = new Mock<IUniqueGlobalLocationNumberRuleService>();
            var target = new ActorFactoryService(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                globalLocationNumberUniquenessService.Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object);

            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, _validDomain, null);
            var globalLocationNumber = new MockedGln();
            var marketRoles = new List<ActorMarketRole> { new(EicFunction.EnergySupplier, Enumerable.Empty<ActorGridArea>()) };

            actorRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
                .ReturnsAsync(new Actor(organization.Id, new MockedGln()));

            // Act
            await target
                .CreateAsync(
                    organization,
                    globalLocationNumber,
                    new ActorName("fake_value"),
                    marketRoles)
                .ConfigureAwait(false);

            // Assert
            globalLocationNumberUniquenessService.Verify(
                x => x.ValidateGlobalLocationNumberAvailableAsync(organization, globalLocationNumber),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NewActor_EnsuresValidRoles()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var overlappingBusinessRolesService = new Mock<IOverlappingEicFunctionsRuleService>();
            var target = new ActorFactoryService(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                overlappingBusinessRolesService.Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object);

            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, _validDomain, null);
            var globalLocationNumber = new MockedGln();
            var meteringPointTypes = new[] { MeteringPointType.D02Analysis };
            var gridAreas = new List<ActorGridArea> { new(meteringPointTypes) };
            var marketRoles = new List<ActorMarketRole> { new(EicFunction.BalanceResponsibleParty, gridAreas) };

            var actor = new Actor(organization.Id, new MockedGln());
            actorRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
                .ReturnsAsync(actor);

            // Act
            await target
                .CreateAsync(
                    organization,
                    globalLocationNumber,
                    new ActorName("fake_value"),
                    marketRoles)
                .ConfigureAwait(false);

            // Assert
            overlappingBusinessRolesService.Verify(
                x => x.ValidateEicFunctionsAcrossActors(It.IsAny<IEnumerable<Actor>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NewActor_EnsuresValidGridAreas()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var allowedGridAreasRuleService = new Mock<IAllowedGridAreasRuleService>();
            var target = new ActorFactoryService(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                allowedGridAreasRuleService.Object);

            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, _validDomain, null);
            var globalLocationNumber = new MockedGln();
            var meteringPointTypes = new[] { MeteringPointType.D02Analysis };
            var gridAreas = new List<ActorGridArea> { new(new GridAreaId(Guid.NewGuid()), meteringPointTypes) };
            var marketRoles = new List<ActorMarketRole> { new(EicFunction.BalanceResponsibleParty, gridAreas) };

            actorRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
                .ReturnsAsync(new Actor(organization.Id, new MockedGln()));

            // Act
            await target
                .CreateAsync(
                    organization,
                    globalLocationNumber,
                    new ActorName("fake_value"),
                    marketRoles)
                .ConfigureAwait(false);

            // Assert
            allowedGridAreasRuleService.Verify(
                x => x.ValidateGridAreas(marketRoles),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NewActor_EnsuresUniqueGridAreas()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var uniqueMarketRoleGridAreaRuleService = new Mock<IUniqueMarketRoleGridAreaRuleService>();
            var target = new ActorFactoryService(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                uniqueMarketRoleGridAreaRuleService.Object,
                new Mock<IAllowedGridAreasRuleService>().Object);

            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, _validDomain, null);
            var globalLocationNumber = new MockedGln();
            var meteringPointTypes = new[] { MeteringPointType.D02Analysis };
            var gridAreas = new List<ActorGridArea> { new(new GridAreaId(Guid.NewGuid()), meteringPointTypes) };
            var marketRoles = new List<ActorMarketRole> { new(EicFunction.BalanceResponsibleParty, gridAreas) };

            var updatedActor = new Actor(organization.Id, new MockedGln());
            actorRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
                .ReturnsAsync(updatedActor);

            // Act
            await target
                .CreateAsync(
                    organization,
                    globalLocationNumber,
                    new ActorName("fake_value"),
                    marketRoles)
                .ConfigureAwait(false);

            // Assert
            uniqueMarketRoleGridAreaRuleService.Verify(
                x => x.ValidateAsync(updatedActor),
                Times.Once);
        }
    }
}
