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
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
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
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IDomainEventRepository>().Object);

            var actorId = Guid.NewGuid();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
                .ReturnsAsync((Actor?)null);

            var command = new UpdateActorCommand(
                actorId,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act + Assert
            await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
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
                overlappingEicFunctionsService.Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IDomainEventRepository>().Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            overlappingEicFunctionsService.Verify(
                x => x.ValidateEicFunctionsAcrossActorsAsync(It.Is<Actor>(a => a.Id == actor.Id)),
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
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                externalActorSynchronizationService.Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                new Mock<IDomainEventRepository>().Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            externalActorSynchronizationService.Verify(
                x => x.ScheduleAsync(It.Is<Guid>(aid => aid == actor.Id.Value)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DomainEvents_ArePublished()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var domainEventRepository = new Mock<IDomainEventRepository>();

            var target = new UpdateActorHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IOverlappingEicFunctionsRuleService>().Object,
                new Mock<IExternalActorSynchronizationRepository>().Object,
                new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
                domainEventRepository.Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorCommand(
                actor.Id.Value,
                new ChangeActorDto("Active", new ActorNameDto(string.Empty), Array.Empty<ActorMarketRoleDto>()));

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            domainEventRepository.Verify(
                x => x.EnqueueAsync(It.IsAny<Actor>()),
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
