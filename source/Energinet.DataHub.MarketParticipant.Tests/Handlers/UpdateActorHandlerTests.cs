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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
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
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new UpdateActorHandler(
                new Mock<IOrganizationRepository>().Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoOrganization_ThrowsNotFoundException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new UpdateActorHandler(
                organizationRepository.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object);

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync((Organization?)null);

            var command = new UpdateActorCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new ChangeActorDto("Active", Array.Empty<MarketRoleDto>()));

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new UpdateActorHandler(
                organizationRepository.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object);

            var organization = new Organization("fake_value");

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new ChangeActorDto("Active", Array.Empty<MarketRoleDto>()));

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_OverlappingRoles_AreValidated()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var overlappingBusinessRolesRuleService = new Mock<IOverlappingBusinessRolesRuleService>();

            var target = new UpdateActorHandler(
                organizationRepository.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IActorIntegrationEventsQueueService>().Object,
                overlappingBusinessRolesRuleService.Object);

            var organization = new Organization("fake_value");
            organization.Actors.Add(new Actor(
                new ExternalActorId(Guid.NewGuid()),
                new GlobalLocationNumber("fake_value")));

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                Guid.NewGuid(),
                Guid.Empty,
                new ChangeActorDto("Active", Array.Empty<MarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            overlappingBusinessRolesRuleService.Verify(
                x => x.ValidateRolesAcrossActors(organization.Actors),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UpdatedActor_DispatchesEvent()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var actorIntegrationEventsQueueService = new Mock<IActorIntegrationEventsQueueService>();

            var target = new UpdateActorHandler(
                organizationRepository.Object,
                UnitOfWorkProviderMock.Create(),
                actorIntegrationEventsQueueService.Object,
                new Mock<IOverlappingBusinessRolesRuleService>().Object);

            var organization = new Organization("fake_value");
            organization.Actors.Add(new Actor(
                new ExternalActorId(Guid.NewGuid()),
                new GlobalLocationNumber("fake_value")));

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);

            var command = new UpdateActorCommand(
                Guid.NewGuid(),
                Guid.Empty,
                new ChangeActorDto("Active", Array.Empty<MarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            actorIntegrationEventsQueueService.Verify(
                x => x.EnqueueActorUpdatedEventAsync(It.IsAny<OrganizationId>(), It.IsAny<Actor>()),
                Times.Once);
        }
    }
}
