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
public sealed class ActorContactAuditLogEntryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    public ActorContactAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var actorContactAuditLogEntryRepository = new ActorContactAuditLogEntryRepository(contextGet);

        // Act
        var actual = await actorContactAuditLogEntryRepository
            .GetAsync(new ActorId(Guid.NewGuid()));

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetAsync_AddContact_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = await _fixture.PrepareActorAsync();
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogEntryRepository = new ActorContactAuditLogEntryRepository(context);
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);

        // Act
        var actual = await actorContactAuditLogEntryRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Name);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContact!.Name);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Email);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContact.Email.Address);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Phone);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContact.Phone.Number);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Created);
        Assert.DoesNotContain(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Deleted);
        Assert.Contains(actorAuditLogs, o => o.ActorId.Value == actor.Id);
    }

    [Fact]
    public async Task GetAsync_AddAndRemoveContact_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = await _fixture.PrepareActorAsync();
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogEntryRepository = new ActorContactAuditLogEntryRepository(context);
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact!);

        // Act
        var actual = await actorContactAuditLogEntryRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Name);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContact!.Name);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Email);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContact.Email.Address);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Phone);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContact.Phone.Number);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Created);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Deleted);
        Assert.Contains(actorAuditLogs, o => o.ActorId.Value == actor.Id);
    }

    [Fact]
    public async Task GetAsync_UpdateContact_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = await _fixture.PrepareActorAsync();
        var actorContact = new ActorContact(new ActorId(actor.Id), "OrgName", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));
        var actorContactChanged = new ActorContact(new ActorId(actor.Id), "new Contact", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorContactAuditLogEntryRepository = new ActorContactAuditLogEntryRepository(context);
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        // Make an audited change.
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact!);
        await actorContactRepository.AddAsync(actorContactChanged);

        // Act
        var actual = await actorContactAuditLogEntryRepository
            .GetAsync(new ActorId(actor.Id));

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Name);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContactChanged!.Name);
        Assert.Contains(actorAuditLogs, o => o.PreviousValue == actorContact!.Name);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Email);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContactChanged.Email.Address);
        Assert.Contains(actorAuditLogs, o => o.PreviousValue == actorContact!.Email.Address);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Phone);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == actorContactChanged!.Phone?.Number);
        Assert.Contains(actorAuditLogs, o => o.PreviousValue == actorContact!.Phone?.Number);
        Assert.Contains(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Created);
        Assert.DoesNotContain(actorAuditLogs, o => o.ActorContactChangeType == ActorContactChangeType.Deleted);
        Assert.Contains(actorAuditLogs, o => o.ActorId.Value == actor.Id);
    }
}
