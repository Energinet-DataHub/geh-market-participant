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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
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
    public sealed class CreateActorContactHandlerTests
    {
        [Fact]
        public async Task Handle_NonExistingActor_Throws()
        {
            // Arrange
            var actorContactRepositoryMock = new Mock<IActorContactRepository>();
            var actorRepositoryMock = new Mock<IActorRepository>();
            var overlappingContactCategoriesRuleService = new Mock<IOverlappingActorContactCategoriesRuleService>();
            var target = new CreateActorContactHandler(
                actorRepositoryMock.Object,
                actorContactRepositoryMock.Object,
                overlappingContactCategoriesRuleService.Object);

            var actor = TestPreparationModels.MockedActor();

            var contact = new ActorContact(
                new ContactId(Guid.NewGuid()),
                actor.Id,
                "fake_value",
                ContactCategory.ElectricalHeating,
                new MockedEmailAddress(),
                null);

            actorContactRepositoryMock
                .Setup(x => x.GetAsync(actor.Id))
                .ReturnsAsync(new[] { contact, contact, contact });

            actorContactRepositoryMock
                .Setup(x => x.AddAsync(It.Is<ActorContact>(y => y.ActorId == actor.Id)))
                .ReturnsAsync(contact.Id);

            var wrongId = Guid.NewGuid();
            var command = new CreateActorContactCommand(
                wrongId,
                new CreateActorContactDto("fake_value", "Default", "fake@value", null));

            // act + assert
            var ex = await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
            Assert.Contains(wrongId.ToString(), ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task Handle_NoOverlappingCategories_MustValidate()
        {
            // Arrange
            var contactRepository = new Mock<IActorContactRepository>();
            var actorRepositoryMock = new Mock<IActorRepository>();
            var overlappingContactCategoriesRuleService = new Mock<IOverlappingActorContactCategoriesRuleService>();
            var target = new CreateActorContactHandler(
                actorRepositoryMock.Object,
                contactRepository.Object,
                overlappingContactCategoriesRuleService.Object);

            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var contact = new ActorContact(
                new ContactId(Guid.NewGuid()),
                actor.Id,
                "fake_value",
                ContactCategory.ElectricalHeating,
                new MockedEmailAddress(),
                null);

            contactRepository
                .Setup(x => x.GetAsync(actor.Id))
                .ReturnsAsync(new[] { contact, contact, contact });

            contactRepository
                .Setup(x => x.AddAsync(It.Is<ActorContact>(y => y.ActorId == actor.Id)))
                .ReturnsAsync(contact.Id);

            var command = new CreateActorContactCommand(
                actor.Id.Value,
                new CreateActorContactDto("fake_value", "Default", "fake@value", null));

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            overlappingContactCategoriesRuleService.Verify(x => x.ValidateCategoriesAcrossContacts(It.Is<IEnumerable<ActorContact>>(y => y.Count() == 4)), Times.Once);
        }

        [Fact]
        public async Task Handle_NewContact_ContactIdReturned()
        {
            // Arrange
            var contactRepository = new Mock<IActorContactRepository>();
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new CreateActorContactHandler(
                actorRepositoryMock.Object,
                contactRepository.Object,
                new Mock<IOverlappingActorContactCategoriesRuleService>().Object);

            var actor = TestPreparationModels.MockedActor();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var contact = new ActorContact(
                new ContactId(Guid.NewGuid()),
                actor.Id,
                "fake_value",
                ContactCategory.ElectricalHeating,
                new MockedEmailAddress(),
                null);

            contactRepository
                .Setup(x => x.AddAsync(It.Is<ActorContact>(y => y.ActorId == actor.Id)))
                .ReturnsAsync(contact.Id);

            var command = new CreateActorContactCommand(
                actor.Id.Value,
                new CreateActorContactDto("fake_value", "Default", "fake@value", null));

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(contact.Id.Value, response.ContactId);
        }
    }
}
