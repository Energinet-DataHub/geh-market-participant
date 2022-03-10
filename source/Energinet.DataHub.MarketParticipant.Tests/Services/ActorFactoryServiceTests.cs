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
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class ActorFactoryServiceTests
    {
        [Fact]
        public async Task CreateAsync_NullOrganization_ThrowsException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                new Mock<IDomainEventRepository>().Object,
                new Mock<IUnitOfWorkProvider>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.CreateAsync(
                null!,
                new GlobalLocationNumber("fake_value"),
                Array.Empty<MarketRole>())).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAsync_NullGln_ThrowsException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                new Mock<IDomainEventRepository>().Object,
                new Mock<IUnitOfWorkProvider>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.CreateAsync(
                new Organization("fake_value"),
                null!,
                Array.Empty<MarketRole>())).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAsync_NullMarketRoles_ThrowsException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                new Mock<IDomainEventRepository>().Object,
                new Mock<IUnitOfWorkProvider>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.CreateAsync(
                new Organization("fake_value"),
                new GlobalLocationNumber("fake_value"),
                null!)).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAsync_NewActor_AddsAndReturnsActor()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                new Mock<IDomainEventRepository>().Object,
                new Mock<IUnitOfWorkProvider>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            var organization = new Organization("fake_value");

            organizationRepository
                .Setup(x => x.GetAsync(organization.Id))
                .ReturnsAsync(organization);

            // Act
            var response = await target
                .CreateAsync(organization, new GlobalLocationNumber("fake_value"), Array.Empty<MarketRole>())
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(organization.Actors);
        }

        [Fact]
        public async Task CreateAsync_NewActor_DispatchesEvent()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var organizationEventDispatcher = new Mock<IDomainEventRepository>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                organizationEventDispatcher.Object,
                new Mock<IUnitOfWorkProvider>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            var expectedId = Guid.NewGuid();
            var organization = new Organization(
                new OrganizationId(expectedId),
                "fake_value",
                Enumerable.Empty<Actor>());

            organizationRepository
                .Setup(x => x.GetAsync(organization.Id))
                .ReturnsAsync(organization);

            // Act
            await target
                .CreateAsync(organization, new GlobalLocationNumber("fake_value"), Array.Empty<MarketRole>())
                .ConfigureAwait(false);

            // Assert
            organizationEventDispatcher.Verify(
                x => x.InsertAsync(It.Is<DomainEvent>(
                    o => o.DomainObjectId == expectedId)),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NewActor_EnsuresGlnUniqueness()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var globalLocationNumberUniquenessService = new Mock<IUniqueGlobalLocationNumberRuleService>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                new Mock<IDomainEventRepository>().Object,
                new Mock<IUnitOfWorkProvider>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                globalLocationNumberUniquenessService.Object,
                new Mock<IActiveDirectoryService>().Object);

            var organization = new Organization("fake_value");
            var globalLocationNumber = new GlobalLocationNumber("fake_value");

            organizationRepository
                .Setup(x => x.GetAsync(organization.Id))
                .ReturnsAsync(organization);

            // Act
            await target
                .CreateAsync(organization, globalLocationNumber, Array.Empty<MarketRole>())
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
            var organizationRepository = new Mock<IOrganizationRepository>();
            var overlappingBusinessRolesService = new Mock<IOverlappingBusinessRolesRuleService>();
            var target = new ActorFactoryService(
                organizationRepository.Object,
                new Mock<IDomainEventRepository>().Object,
                new Mock<IUnitOfWorkProvider>().Object,
                overlappingBusinessRolesService.Object,
                new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            var organization = new Organization("fake_value");
            var globalLocationNumber = new GlobalLocationNumber("fake_value");
            var marketRoles = new[] { new MarketRole(EicFunction.BalanceResponsibleParty) };

            organizationRepository
                .Setup(x => x.GetAsync(organization.Id))
                .ReturnsAsync(organization);

            // Act
            await target
                .CreateAsync(organization, globalLocationNumber, marketRoles)
                .ConfigureAwait(false);

            // Assert
            overlappingBusinessRolesService.Verify(
                x => x.ValidateRolesAcrossActors(organization.Actors, marketRoles),
                Times.Once);
        }
    }
}
