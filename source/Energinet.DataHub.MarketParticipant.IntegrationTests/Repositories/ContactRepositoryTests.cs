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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class ContactRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public ContactRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAsync_ContactNotExists_ReturnsNull()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var contactRepository = new ContactRepository(context);

            // Act
            var testContact = await contactRepository
                .GetAsync(new ContactId(Guid.NewGuid()))
                .ConfigureAwait(false);

            // Assert
            Assert.Null(testContact);
        }

        [Fact]
        public async Task GetAsync_ForAnOrganization_ReturnsNull()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var organizationRepository = scope.GetInstance<IOrganizationRepository>();
            var organizationId = await organizationRepository
                .AddOrUpdateAsync(new Organization("Test Organization"))
                .ConfigureAwait(false);

            var contactRepository = new ContactRepository(context);

            for (var i = 0; i < 5; i++)
            {
                await contactRepository
                    .AddAsync(new Contact(
                        organizationId,
                        "fake_value",
                        ContactCategory.Charges,
                        new EmailAddress("fake@fake.dk"),
                        new PhoneNumber("1234567")))
                    .ConfigureAwait(false);
            }

            // Act
            var testContacts = await contactRepository
                .GetAsync(organizationId)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(5, testContacts.Count());
        }

        [Fact]
        public async Task AddAsync_OneContact_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var organizationRepository = scope.GetInstance<IOrganizationRepository>();
            var organizationId = await organizationRepository
                .AddOrUpdateAsync(new Organization("Test Organization"))
                .ConfigureAwait(false);

            var contactRepository = new ContactRepository(context);

            var testContact = new Contact(
                organizationId,
                "fake_value",
                ContactCategory.Charges,
                new EmailAddress("fake@fake.dk"),
                new PhoneNumber("1234567"));

            // Act
            var contactId = await contactRepository.AddAsync(testContact).ConfigureAwait(false);
            var newContact = await contactRepository.GetAsync(contactId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newContact);
            Assert.NotEqual(Guid.Empty, newContact?.Id.Value);
            Assert.NotEqual(Guid.Empty, newContact?.OrganizationId.Value);
            Assert.Equal(testContact.Category, newContact?.Category);
            Assert.Equal(testContact.EmailAddress.Address, newContact?.EmailAddress.Address);
            Assert.Equal(testContact.Name, newContact?.Name);
            Assert.Equal(testContact.Phone?.Number, newContact?.Phone?.Number);
        }

        [Fact]
        public async Task RemoveAsync_OneContact_RemovesContact()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var organizationRepository = scope.GetInstance<IOrganizationRepository>();
            var organizationId = await organizationRepository
                .AddOrUpdateAsync(new Organization("Test Organization"))
                .ConfigureAwait(false);

            var contactRepository = new ContactRepository(context);

            var testContact = new Contact(
                organizationId,
                "fake_value",
                ContactCategory.Charges,
                new EmailAddress("fake@fake.dk"),
                new PhoneNumber("1234567"));

            var contactId = await contactRepository.AddAsync(testContact).ConfigureAwait(false);
            var newContact = await contactRepository.GetAsync(contactId).ConfigureAwait(false);

            // Act
            await contactRepository.RemoveAsync(newContact!).ConfigureAwait(false);
            var deletedContact = await contactRepository.GetAsync(contactId).ConfigureAwait(false);

            // Assert
            Assert.Null(deletedContact);
        }

        [Fact]
        public async Task GetAsync_DifferentContexts_CanReadBack()
        {
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();

            var organizationRepository = scope.GetInstance<IOrganizationRepository>();
            var organizationId = await organizationRepository
                .AddOrUpdateAsync(new Organization("Test Organization"))
                .ConfigureAwait(false);

            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var contextReadback = _fixture.DatabaseManager.CreateDbContext();

            var contactRepository = new ContactRepository(context);
            var contactRepositoryReadback = new ContactRepository(contextReadback);

            var testContact = new Contact(
                organizationId,
                "fake_value",
                ContactCategory.Charges,
                new EmailAddress("fake@fake.dk"),
                new PhoneNumber("1234567"));

            // Act
            var contactId = await contactRepository.AddAsync(testContact).ConfigureAwait(false);
            var newContact = await contactRepositoryReadback.GetAsync(contactId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newContact);
            Assert.NotEqual(Guid.Empty, newContact?.Id.Value);
            Assert.Equal(testContact.Category, newContact?.Category);
            Assert.Equal(testContact.EmailAddress.Address, newContact?.EmailAddress.Address);
            Assert.Equal(testContact.Name, newContact?.Name);
            Assert.Equal(testContact.Phone?.Number, newContact?.Phone?.Number);
        }
    }
}
