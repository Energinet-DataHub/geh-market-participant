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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateActorHandlerTests
    {
        private readonly Address _validAddress = new(
            "test Street",
            "1",
            "1111",
            "Test City",
            "Test Country");

        private readonly BusinessRegisterIdentifier _validCvrBusinessRegisterIdentifier = new("12345678");

        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                new Mock<IOrganizationExistsHelperService>().Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorIdConfigurationService>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                organizationExistsHelperService.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorIdConfigurationService>().Object);

            var organizationId = Guid.NewGuid();
            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress);

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                organizationId,
                Guid.NewGuid(),
                new ChangeActorDto("Active", Array.Empty<Guid>(), Array.Empty<MarketRoleDto>(), Array.Empty<string>()));

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_AllowedGridAreas_AreValidated()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var allowedGridAreasRuleService = new Mock<IAllowedGridAreasRuleService>();

            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                organizationExistsHelperService.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                allowedGridAreasRuleService.Object,
                new Mock<IExternalActorIdConfigurationService>().Object);

            var organizationId = Guid.NewGuid();
            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress);

            organization.Actors.Add(new Actor(new ActorNumber("fake_value")));

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                organizationId,
                Guid.Empty,
                new ChangeActorDto("Active", Array.Empty<Guid>(), Array.Empty<MarketRoleDto>(), Array.Empty<string>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            allowedGridAreasRuleService.Verify(
                x => x.ValidateGridAreas(It.IsAny<IEnumerable<GridAreaId>>(), It.IsAny<IEnumerable<MarketRole>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_OverlappingRoles_AreValidated()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var overlappingBusinessRolesRuleService = new Mock<IOverlappingBusinessRolesRuleService>();

            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                organizationExistsHelperService.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                overlappingBusinessRolesRuleService.Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorIdConfigurationService>().Object);

            var organizationId = Guid.NewGuid();
            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress);

            organization.Actors.Add(new Actor(new ActorNumber("fake_value")));

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                organizationId,
                Guid.Empty,
                new ChangeActorDto("Active", Array.Empty<Guid>(), Array.Empty<MarketRoleDto>(), Array.Empty<string>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            overlappingBusinessRolesRuleService.Verify(
                x => x.ValidateRolesAcrossActors(organization.Actors),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ExternalActorId_IsGenerated()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var externalActorIdGenerationService = new Mock<IExternalActorIdConfigurationService>();

            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                organizationExistsHelperService.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                externalActorIdGenerationService.Object);

            var organizationId = Guid.NewGuid();
            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress);

            organization.Actors.Add(new Actor(new ActorNumber("fake_value")));

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                organizationId,
                Guid.Empty,
                new ChangeActorDto("Active", Array.Empty<Guid>(), Array.Empty<MarketRoleDto>(), Array.Empty<string>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            externalActorIdGenerationService.Verify(
                x => x.AssignExternalActorIdAsync(It.IsAny<Actor>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UpdatedActor_DispatchesEvent()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var actorIntegrationEventsQueueService = new Mock<IActorIntegrationEventsQueueService>();

            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                organizationExistsHelperService.Object,
                UnitOfWorkProviderMock.Create(),
                actorIntegrationEventsQueueService.Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorIdConfigurationService>().Object);

            var organizationId = Guid.NewGuid();
            var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress);

            organization.Actors.Add(new Actor(new ActorNumber("fake_value")));

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                organizationId,
                Guid.Empty,
                new ChangeActorDto("Active", Array.Empty<Guid>(), Array.Empty<MarketRoleDto>(), Array.Empty<string>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            actorIntegrationEventsQueueService.Verify(
                x => x.EnqueueActorUpdatedEventAsync(It.IsAny<OrganizationId>(), It.IsAny<Actor>()),
                Times.Once);
        }
    }
}
