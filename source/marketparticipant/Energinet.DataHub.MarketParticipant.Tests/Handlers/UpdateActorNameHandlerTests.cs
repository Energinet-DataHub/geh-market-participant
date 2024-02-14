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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateActorNameHandlerTests
    {
        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new UpdateActorNameHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IDomainEventRepository>().Object);

            var actorId = Guid.NewGuid();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
                .ReturnsAsync((Actor?)null);

            var command = new UpdateActorNameCommand(
                actorId,
                new ActorNameDto("NewActorName"));

            // Act + Assert
            await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RepositoryCalledWithNewName_Ok()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var domainEventRepository = new Mock<IDomainEventRepository>();

            var target = new UpdateActorNameHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                domainEventRepository.Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorNameCommand(
                actor.Id.Value,
                new ActorNameDto("NewActorName"));

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            actorRepositoryMock
                .Verify(e => e.AddOrUpdateAsync(It.Is<Actor>(a => a.Name.Value == "NewActorName")));
        }

        [Fact]
        public async Task Handle_DomainEvents_ArePublished()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var domainEventRepository = new Mock<IDomainEventRepository>();

            var target = new UpdateActorNameHandler(
                actorRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                domainEventRepository.Object);

            var actor = MockActor(actorRepositoryMock);

            var command = new UpdateActorNameCommand(
                actor.Id.Value,
                new ActorNameDto("NewActorName"));

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
