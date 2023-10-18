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
public sealed class ActorAuditLogEntryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    public ActorAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var actorAuditLogEntryRepository = new ActorAuditLogEntryRepository(contextGet);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(new ActorId(Guid.NewGuid()));

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetAsync_ChangeName_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));
        var orgValue = actor.Name.Value;
        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();

        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorAuditLogEntryRepository = new ActorAuditLogEntryRepository(context);

        // Make an audited change.
        var actorId = await actorRepository.AddOrUpdateAsync(actor);
        actor = await actorRepository.GetAsync(actorId);
        actor!.Name = new ActorName("Test Name 2");
        await actorRepository.AddOrUpdateAsync(actor!);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(actorId);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.ActorChangeType == ActorChangeType.Name);
        Assert.Contains(actorAuditLogs, o => o.Value == "Test Name 2");
        Assert.Contains(actorAuditLogs, o => o.Value == orgValue);
        Assert.Contains(actorAuditLogs, o => o.ActorId == actorId);
    }

    [Fact]
    public async Task GetAsync_ChangeStatus_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));
        var orgValue = actor.Status;
        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();

        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorAuditLogEntryRepository = new ActorAuditLogEntryRepository(context);

        // Make an audited change.
        var actorId = await actorRepository.AddOrUpdateAsync(actor);
        actor = await actorRepository.GetAsync(actorId);
        actor!.Status = ActorStatus.Active;
        await actorRepository.AddOrUpdateAsync(actor!);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(actorId);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.ActorChangeType == ActorChangeType.Status);
        Assert.Contains(actorAuditLogs, o => o.Value == ActorStatus.Active.ToString());
        Assert.Contains(actorAuditLogs, o => o.Value == orgValue.ToString());
        Assert.Contains(actorAuditLogs, o => o.ActorId == actorId);
    }

    [Fact]
    public async Task GetAsync_ContactEmailChanged_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));

        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();

        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorAuditLogEntryRepository = new ActorAuditLogEntryRepository(context);

        // Make an audited change.
        var actorId = await actorRepository.AddOrUpdateAsync(actor);
        var actorContact = new ActorContact(actorId, "OrgName", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));
        var actorContactChanged = new ActorContact(actorId, "new Contact", ContactCategory.Default, new MockedEmailAddress(), new PhoneNumber("1234567"));
        var contactId = await actorContactRepository.AddAsync(actorContact);
        actorContact = await actorContactRepository.GetAsync(contactId);
        await actorContactRepository.RemoveAsync(actorContact);
        await actorContactRepository.AddAsync(actorContactChanged);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(actorId);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.ActorChangeType == ActorChangeType.ContactEmail);
        Assert.Contains(actorAuditLogs, o => o.Value == actorContact.Email.ToString());
        Assert.Contains(actorAuditLogs, o => o.Value == actorContactChanged.Email.ToString());
        Assert.All(actorAuditLogs, o => Assert.Equal(actorId, o.ActorId));
    }
}
