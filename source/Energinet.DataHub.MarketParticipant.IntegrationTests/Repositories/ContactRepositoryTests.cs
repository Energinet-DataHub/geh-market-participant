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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
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
        public async Task AddOrUpdateAsync_OneContact_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var contactRepository = new ContactRepository(context);
            var testContact = new Contact(
                ContactCategory.Charges,
                new ContactName("fake_value"),
                new ContactEmail("fake@fake.dk"),
                new ContactPhone("1234567"));

            // Act
            var contactId = await contactRepository.AddOrUpdateAsync(testContact).ConfigureAwait(false);
            var newContact = await contactRepository.GetAsync(contactId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newContact);
            Assert.NotEqual(Guid.Empty, newContact?.Id.Value);
            Assert.Equal(testContact.Category, newContact?.Category);
            Assert.Equal(testContact.Email.Value, newContact?.Email.Value);
            Assert.Equal(testContact.Name.Value, newContact?.Name.Value);
            Assert.Equal(testContact.Phone.Value, newContact?.Phone.Value);
        }

        [Fact]
        public async Task AddOrUpdateAsync_ContactChanged_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var contactRepository = new ContactRepository(context);
            var testContact = new Contact(
                ContactCategory.Charges,
                new ContactName("fake_value"),
                new ContactEmail("fake@fake.dk"),
                new ContactPhone("1234567"));

            // Act
            var contactId = await contactRepository.AddOrUpdateAsync(testContact).ConfigureAwait(false);
            var newContact = await contactRepository.GetAsync(contactId).ConfigureAwait(false);
            newContact = new Contact(
                newContact!.Id,
                ContactCategory.Reminder,
                newContact.Name,
                new ContactEmail("fake2@fake.dk"),
                newContact.Phone);

            await contactRepository.AddOrUpdateAsync(newContact).ConfigureAwait(false);
            newContact = await contactRepository.GetAsync(contactId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newContact);
            Assert.NotEqual(Guid.Empty, newContact?.Id.Value);
            Assert.Equal("fake2@fake.dk", newContact?.Email.Value);
            Assert.Equal(ContactCategory.Reminder, newContact?.Category);
        }

        [Fact]
        public async Task GetAsync_DifferentContexts_CanReadBack()
        {
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var contextReadback = _fixture.DatabaseManager.CreateDbContext();
            var contactRepository = new ContactRepository(context);
            var contactRepositoryReadback = new ContactRepository(contextReadback);

            var testContact = new Contact(
                ContactCategory.Charges,
                new ContactName("fake_value"),
                new ContactEmail("fake@fake.dk"),
                new ContactPhone("1234567"));

            // Act
            var contactId = await contactRepository.AddOrUpdateAsync(testContact).ConfigureAwait(false);
            var newContact = await contactRepositoryReadback.GetAsync(contactId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newContact);
            Assert.NotEqual(Guid.Empty, newContact?.Id.Value);
            Assert.Equal(testContact.Category, newContact?.Category);
            Assert.Equal(testContact.Email.Value, newContact?.Email.Value);
            Assert.Equal(testContact.Name.Value, newContact?.Name.Value);
            Assert.Equal(testContact.Phone.Value, newContact?.Phone.Value);
        }
    }
}
