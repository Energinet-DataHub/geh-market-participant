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
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorContactAuditLogRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ActorContactAuditLogRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_NoAuditLogs_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var contextGet = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = new ActorContactAuditLogRepository(contextGet);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(Guid.NewGuid()));

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetAsync_AddContact_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contact
        var actor = await _fixture.PrepareActorAsync();
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        await actorContactRepository.AddAsync(actorContact);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_AddAndRemoveContact_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contact
        var actor = await _fixture.PrepareActorAsync();
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("12345678"));

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact!);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded);
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_UpdateContact_WithNameChange_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contact
        var actor = await _fixture.PrepareActorAsync();
        var email = new MockedEmailAddress();
        var phone = new PhoneNumber("12345678");
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, email, phone);
        var actorContactChanged = new ActorContact(new ActorId(actor.Id), "new Contact", ContactCategory.Default, email, phone);

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact!);
        await actorContactRepository.AddAsync(actorContactChanged);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && o.CurrentValue == actorContactChanged.Name && o.PreviousValue == actorContact!.Name);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_UpdateContact_WithEmailChange_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contact
        var actor = await _fixture.PrepareActorAsync();
        var phone = new PhoneNumber("12345678");
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), phone);
        var actorContactChanged = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), phone);

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact!);
        await actorContactRepository.AddAsync(actorContactChanged);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && o.CurrentValue == actorContactChanged.Email.Address && o.PreviousValue == actorContact!.Email.Address);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_UpdateContact_WithPhoneChange_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contact
        var actor = await _fixture.PrepareActorAsync();
        var email = new MockedEmailAddress();
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, email, new PhoneNumber("12345678"));
        var actorContactChanged = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, email, new PhoneNumber("87654321"));

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact!);
        await actorContactRepository.AddAsync(actorContactChanged);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded);
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && o.CurrentValue == actorContactChanged.Phone?.Number && o.PreviousValue == actorContact!.Phone?.Number);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_AddContacts_MultipleCategories_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contacts
        var actor = await _fixture.PrepareActorAsync();
        var email = new MockedEmailAddress();
        var phone = new PhoneNumber("12345678");
        var actorContact = new ActorContact(new ActorId(actor.Id), "NameDefault", ContactCategory.Default, email, phone);
        var actorContactOtherCategory = new ActorContact(new ActorId(actor.Id), "NameCharges", ContactCategory.Charges, email, phone);

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Arrange - Make an audited change.
        await actorContactRepository.AddAsync(actorContact);
        await actorContactRepository.AddAsync(actorContactOtherCategory);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the DEFAULT Category contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContact.Category.ToString());

        // Assert - Verify that the CHARGES Category contact has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContactOtherCategory.Category.ToString());

        // Assert - Verify that none of the categories contains a deleted audit log
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_AddAndUpdateOneContact_WithNameChange_WithMultipleCategories_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contacts
        var actor = await _fixture.PrepareActorAsync();
        var email = new MockedEmailAddress();
        var phone = new PhoneNumber("12345678");
        var actorContact = new ActorContact(new ActorId(actor.Id), "NameDefault", ContactCategory.Default, email, phone);
        var actorContactOtherCategory = new ActorContact(new ActorId(actor.Id), "NameCharges", ContactCategory.Charges, email, phone);
        var actorContactChangedOtherCategory = new ActorContact(new ActorId(actor.Id), "NameChargesChanged", ContactCategory.Charges, email, phone);

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Arrange - Make an audited change.
        await actorContactRepository.AddAsync(actorContact);
        var contactId = await actorContactRepository.AddAsync(actorContactOtherCategory);
        actorContactOtherCategory = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContactOtherCategory!);
        await actorContactRepository.AddAsync(actorContactChangedOtherCategory);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the DEFAULT category contacts has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContact.Category.ToString());

        // Assert - Verify that the CHARGES category contacts has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContactOtherCategory!.Category.ToString());
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && o.CurrentValue == actorContactChangedOtherCategory.Name && actorContactOtherCategory!.Name == o.PreviousValue);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && !string.IsNullOrEmpty(o.PreviousValue));

        // Assert - Verify that none of the categories contains a deleted audit log
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved);
    }

    [Fact]
    public async Task GetAsync_AddAndUpdateThenDeleteOneContact_WithNameChangedBeforeDelete_WithMultipleCategoriesOfContacts_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contacts
        var actor = await _fixture.PrepareActorAsync();
        var email = new MockedEmailAddress();
        var phone = new PhoneNumber("12345678");
        var actorContact = new ActorContact(new ActorId(actor.Id), "NameDefault", ContactCategory.Default, email, phone);
        var actorContactOtherCategory = new ActorContact(new ActorId(actor.Id), "NameCharges", ContactCategory.Charges, email, phone);
        var actorContactChangedOtherCategory = new ActorContact(new ActorId(actor.Id), "NameChargesChanged", ContactCategory.Charges, email, phone);

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Arrange - Add non audited contact, except for create
        await actorContactRepository.AddAsync(actorContact);

        // Arrange - Make an audited change To Charges contact.
        var contactId = await actorContactRepository.AddAsync(actorContactOtherCategory);
        actorContactOtherCategory = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContactOtherCategory!);

        // Arrange - Delete the audited contact
        contactId = await actorContactRepository.AddAsync(actorContactChangedOtherCategory);
        actorContactChangedOtherCategory = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContactChangedOtherCategory!);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the DEFAULT category contacts has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContact.Category.ToString());

        // Assert - Verify that the CHARGES category contacts has expected  audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContactOtherCategory!.Category.ToString());
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && o.CurrentValue == actorContactChangedOtherCategory!.Name && actorContactOtherCategory!.Name == o.PreviousValue);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && !string.IsNullOrEmpty(o.PreviousValue));

        // Assert - Verify that expected deleted logs are present
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved && o.PreviousValue == actorContactChangedOtherCategory!.Category.ToString());
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved && o.PreviousValue == actorContact.Category.ToString());
    }

    [Fact]
    public async Task GetAsync_AddAndUpdateContacts_NameChangedForBoth_MultipleCategories_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup user
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup contacts
        var actor = await _fixture.PrepareActorAsync();
        var email = new MockedEmailAddress();
        var phone = new PhoneNumber("12345678");
        var actorContact = new ActorContact(new ActorId(actor.Id), "NameDefault", ContactCategory.Default, email, phone);
        var actorContactChanged = new ActorContact(new ActorId(actor.Id), "NameDefaultChanged", ContactCategory.Default, email, phone);
        var actorContactOtherCategory = new ActorContact(new ActorId(actor.Id), "NameCharges", ContactCategory.Charges, email, phone);
        var actorContactChangedOtherCategory = new ActorContact(new ActorId(actor.Id), "NameChargesChanged", ContactCategory.Charges, email, phone);

        // Arrange - Setup repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorContactAuditLogRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Arrange - Make an audited change one DEFAULT contact
        var contactChangedId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactChangedId);
        await actorContactRepository.RemoveAsync(actorContact!);
        await actorContactRepository.AddAsync(actorContactChanged);

        // Arrange - Make an audited change one CHARGES contact
        contactChangedId = await actorContactRepository.AddAsync(actorContactOtherCategory);
        actorContactOtherCategory = await actorContactRepository.GetAsync(contactChangedId);
        await actorContactRepository.RemoveAsync(actorContactOtherCategory!);
        await actorContactRepository.AddAsync(actorContactChangedOtherCategory);

        // Act
        var actual = await actorContactAuditLogRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorContactAuditLogs = actual.ToList();

        // Assert - Verify that the basics are as expected
        Assert.NotEmpty(actorContactAuditLogs);
        Assert.All(actorContactAuditLogs, o => Assert.Equal(user.Id, o.AuditIdentity.Value));

        // Assert - Verify that the DEFAULT category contacts has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContact!.Category.ToString());
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && o.CurrentValue == actorContactChanged.Name && actorContact!.Name == o.PreviousValue);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && !string.IsNullOrEmpty(o.PreviousValue));

        // Assert - Verify that the CHARGES category contacts has expected audit logs
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryAdded && o.CurrentValue == actorContactOtherCategory!.Category.ToString());
        Assert.Contains(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactName && o.CurrentValue == actorContactChangedOtherCategory.Name && actorContactOtherCategory!.Name == o.PreviousValue);
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactEmail && !string.IsNullOrEmpty(o.PreviousValue));
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactPhone && !string.IsNullOrEmpty(o.PreviousValue));

        // Assert - Verify that only the expected deleted logs are present
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved && o.PreviousValue == actorContactChangedOtherCategory.Category.ToString());
        Assert.DoesNotContain(actorContactAuditLogs, o => o.Change == ActorAuditedChange.ContactCategoryRemoved && o.PreviousValue == actorContact!.Category.ToString());
    }
}
