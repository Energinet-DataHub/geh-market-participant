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
    public sealed class GetActorContactsHandlerTests
    {
        [Fact]
        public async Task Handle_NonExistingActor_Throws()
        {
            var actorRepositoryMock = new Mock<IActorRepository>();
            var contactRepository = new Mock<IActorContactRepository>();
            var target = new GetActorContactsHandler(
                actorRepositoryMock.Object,
                contactRepository.Object);

            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var expected = new ActorContact(
                new ContactId(Guid.NewGuid()),
                actor.Id,
                "fake_value",
                ContactCategory.EndOfSupply,
                new MockedEmailAddress(),
                new PhoneNumber("1234"));

            contactRepository
                .Setup(x => x.GetAsync(actor.Id))
                .ReturnsAsync(new[] { expected });

            var wrongId = Guid.NewGuid();
            var command = new GetActorContactsCommand(wrongId);

            // act + assert
            var ex = await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
            Assert.Contains(wrongId.ToString(), ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task Handle_HasContacts_ReturnsContacts()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var contactRepository = new Mock<IActorContactRepository>();
            var target = new GetActorContactsHandler(
                actorRepositoryMock.Object,
                contactRepository.Object);

            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(x => x.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var expected = new ActorContact(
                new ContactId(Guid.NewGuid()),
                actor.Id,
                "fake_value",
                ContactCategory.EndOfSupply,
                new MockedEmailAddress(),
                new PhoneNumber("1234"));

            contactRepository
                .Setup(x => x.GetAsync(actor.Id))
                .ReturnsAsync(new[] { expected });

            var command = new GetActorContactsCommand(actor.Id.Value);

            // Act
            var response = await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotEmpty(response.Contacts);

            var actualContact = response.Contacts.Single();
            Assert.Equal(expected.Id.Value, actualContact.ContactId);
            Assert.Equal(expected.Name, actualContact.Name);
            Assert.Equal(expected.Category.ToString(), actualContact.Category);
            Assert.Equal(expected.Email.Address, actualContact.Email);
            Assert.Equal(expected.Phone?.Number, actualContact.Phone);
        }
    }
}
