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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
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
    public sealed class DeleteActorContactHandlerTests
    {
        [Fact]
        public async Task Handle_NonExistingActor_Throws()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var contactRepository = new Mock<IActorContactRepository>();
            var target = new DeleteActorContactHandler(
                actorRepositoryMock.Object,
                contactRepository.Object);

            var contactId = new ContactId(Guid.NewGuid());
            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            contactRepository
                .Setup(x => x.GetAsync(contactId))
                .ReturnsAsync((ActorContact?)null);

            var wrongId = Guid.NewGuid();
            var command = new DeleteActorContactCommand(wrongId, contactId.Value);

            // act + assert
            var ex = await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
            Assert.Contains(wrongId.ToString(), ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task Handle_NoContact_DoesNothing()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var contactRepository = new Mock<IActorContactRepository>();
            var target = new DeleteActorContactHandler(
                actorRepositoryMock.Object,
                contactRepository.Object);

            var contactId = new ContactId(Guid.NewGuid());
            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            contactRepository
                .Setup(x => x.GetAsync(contactId))
                .ReturnsAsync((ActorContact?)null);

            var command = new DeleteActorContactCommand(actor.Id.Value, contactId.Value);

            // Act + Assert
            await target.Handle(command, CancellationToken.None);
        }

        [Fact]
        public async Task Handle_OneContact_IsDeleted()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var contactRepository = new Mock<IActorContactRepository>();
            var target = new DeleteActorContactHandler(
                actorRepositoryMock.Object,
                contactRepository.Object);

            var contactId = new ContactId(Guid.NewGuid());
            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var contactToDelete = new ActorContact(
                contactId,
                actor.Id,
                "fake_value",
                ContactCategory.EndOfSupply,
                new MockedEmailAddress(),
                null);

            contactRepository
                .Setup(x => x.GetAsync(contactId))
                .ReturnsAsync(contactToDelete);

            var command = new DeleteActorContactCommand(actor.Id.Value, contactId.Value);

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            contactRepository.Verify(x => x.RemoveAsync(contactToDelete), Times.Once);
        }
    }
}
