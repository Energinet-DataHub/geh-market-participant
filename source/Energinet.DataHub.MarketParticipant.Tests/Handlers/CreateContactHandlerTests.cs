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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateContactHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new CreateContactHandler(
                new Mock<IOrganizationExistsHelperService>().Object,
                new Mock<IContactRepository>().Object,
                new Mock<IOverlappingContactCategoriesRuleService>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoOverlappingCategories_MustValidate()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var contactRepository = new Mock<IContactRepository>();
            var overlappingContactCategoriesRuleService = new Mock<IOverlappingContactCategoriesRuleService>();
            var target = new CreateContactHandler(
                organizationExistsHelperService.Object,
                contactRepository.Object,
                overlappingContactCategoriesRuleService.Object);

            var orgId = new OrganizationId(Guid.NewGuid());
            var validBusinessRegisterIdentifier = new BusinessRegisterIdentifier("123");
            var validAddress = new Address(
                "test Street",
                "1",
                "1111",
                "Test City",
                "Test Country");
            const string orgName = "SomeName";

            var organization = new Organization(
                orgId,
                orgName,
                Enumerable.Empty<Actor>(),
                validBusinessRegisterIdentifier,
                validAddress,
                "Test Comment");

            var contact = new Contact(
                new ContactId(Guid.NewGuid()),
                orgId,
                "fake_value",
                ContactCategory.ElectricalHeating,
                new EmailAddress("john@doe"),
                null);

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(orgId.Value))
                .ReturnsAsync(organization);

            contactRepository
                .Setup(x => x.GetAsync(orgId))
                .ReturnsAsync(new[] { contact, contact, contact });

            contactRepository
                .Setup(x => x.AddAsync(It.Is<Contact>(y => y.OrganizationId == orgId)))
                .ReturnsAsync(contact.Id);

            var command = new CreateContactCommand(
                orgId.Value,
                new CreateContactDto("fake_value", "Default", "fake@value", null));

            // Act
            await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            overlappingContactCategoriesRuleService.Verify(x => x.ValidateCategoriesAcrossContacts(It.Is<IEnumerable<Contact>>(y => y.Count() == 4)), Times.Once);
        }

        [Fact]
        public async Task Handle_NewContact_ContactIdReturned()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var contactRepository = new Mock<IContactRepository>();
            var target = new CreateContactHandler(
                organizationExistsHelperService.Object,
                contactRepository.Object,
                new Mock<IOverlappingContactCategoriesRuleService>().Object);

            var validBusinessRegisterIdentifier = new BusinessRegisterIdentifier("123");
            var validAddress = new Address(
                "test Street",
                "1",
                "1111",
                "Test City",
                "Test Country");
            var orgId = new OrganizationId(Guid.NewGuid());
            const string orgName = "SomeName";

            var organization = new Organization(
                orgId,
                orgName,
                Enumerable.Empty<Actor>(),
                validBusinessRegisterIdentifier,
                validAddress,
                "Test Comment");

            var contact = new Contact(
                new ContactId(Guid.NewGuid()),
                orgId,
                "fake_value",
                ContactCategory.ElectricalHeating,
                new EmailAddress("john@doe"),
                null);

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(orgId.Value))
                .ReturnsAsync(organization);

            contactRepository
                .Setup(x => x.AddAsync(It.Is<Contact>(y => y.OrganizationId == orgId)))
                .ReturnsAsync(contact.Id);

            var command = new CreateContactCommand(
                orgId.Value,
                new CreateContactDto("fake_value", "Default", "fake@value", null));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(contact.Id.Value, response.ContactId);
        }
    }
}
