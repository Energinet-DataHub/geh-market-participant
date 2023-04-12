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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Application.Helpers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateActorHandlerTests
    {
        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new UpdateActorHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IChangesToActorHelper>().Object,
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IActorStatusMarketRolesRuleService>().Object);

            var actorId = Guid.NewGuid();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
                .ReturnsAsync((Actor?)null);

            var command = new UpdateActorCommand(
                actorId,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_AllowedGridAreas_AreValidated()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var allowedGridAreasRuleService = new Mock<IAllowedGridAreasRuleService>();

            var target = new UpdateActorHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IChangesToActorHelper>().Object,
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                allowedGridAreasRuleService.Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IActorStatusMarketRolesRuleService>().Object);

            var actor = MockActor(actorRepositoryMock);

            var meteringPoints = new[]
            {
                MeteringPointType.D02Analysis.ToString(), MeteringPointType.E17Consumption.ToString(),
                MeteringPointType.E17Consumption.ToString()
            };
            var gridAreas = new[] { new ActorGridAreaDto(Guid.NewGuid(), meteringPoints) };
            var marketRoles = new[] { new ActorMarketRoleDto("EnergySupplier", gridAreas, string.Empty) };

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), marketRoles));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            allowedGridAreasRuleService.Verify(
                x => x.ValidateGridAreas(It.IsAny<IEnumerable<ActorMarketRole>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_OverlappingRoles_AreValidated()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var overlappingEicFunctionsService = new Mock<IOverlappingEicFunctionsRuleService>();
            var target = new UpdateActorHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IChangesToActorHelper>().Object,
                new Mock<IActorIntegrationEventsQueueService>().Object,
                overlappingEicFunctionsService.Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IActorStatusMarketRolesRuleService>().Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            overlappingEicFunctionsService.Verify(
                x => x.ValidateEicFunctionsAcrossActors(It.Is<IEnumerable<Actor>>(col => col.Any(a => a.Id == actor.Id))),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ExternalActorId_SyncIsScheduled()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var externalActorSynchronizationService = new Mock<IExternalActorSynchronizationRepository>();

            var target = new UpdateActorHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IChangesToActorHelper>().Object,
                new Mock<IActorIntegrationEventsQueueService>().Object,
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                externalActorSynchronizationService.Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IActorStatusMarketRolesRuleService>().Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            externalActorSynchronizationService.Verify(
                x => x.ScheduleAsync(It.Is<Guid>(aid => aid == actor.Id.Value)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UpdatedActor_DispatchesEvent()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var actorIntegrationEventsQueueService = new Mock<IActorIntegrationEventsQueueService>();

            var target = new UpdateActorHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IChangesToActorHelper>().Object,
                actorIntegrationEventsQueueService.Object,
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IAllowedGridAreasRuleService>().Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IActorStatusMarketRolesRuleService>().Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            actorIntegrationEventsQueueService.Verify(
                x => x.EnqueueActorUpdatedEventAsync(It.IsAny<Actor>()),
                Times.Once);
        }

        private static Actor MockActor(Mock<IActorRepository> actorRepositoryMock)
        {
            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            return actor;
        }
    }
}
