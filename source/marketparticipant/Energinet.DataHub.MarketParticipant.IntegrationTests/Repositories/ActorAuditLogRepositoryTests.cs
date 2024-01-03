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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorAuditLogRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ActorAuditLogRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var actorAuditLogEntryRepository = new ActorAuditLogRepository(contextGet);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(new ActorId(Guid.NewGuid()));

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetAsync_Created_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup User
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup Organization and Actor
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));

        // Arrange - Setup Repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IActorAuditLogRepository>();

        // Make an audited change.
        var result = await actorRepository.AddOrUpdateAsync(actor);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(result.Value);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs);
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o is { Change: ActorAuditedChange.Status, IsInitialAssignment: true });
        Assert.Contains(actorAuditLogs, o => o is { Change: ActorAuditedChange.Name, IsInitialAssignment: true });
    }

    [Fact]
    public async Task GetAsync_ChangeName_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup User
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup Organization and Actor
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));
        var orgValue = actor.Name.Value;

        // Arrange - Setup Repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IActorAuditLogRepository>();

        // Make an audited change.
        var result = await actorRepository.AddOrUpdateAsync(actor);
        actor = await actorRepository.GetAsync(result.Value);
        actor!.Name = new ActorName("Test Name 2");
        await actorRepository.AddOrUpdateAsync(actor);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(result.Value);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs);
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.Change == ActorAuditedChange.Name);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == "Test Name 2");
        Assert.Contains(actorAuditLogs, o => o.PreviousValue == orgValue);
    }

    [Fact]
    public async Task GetAsync_ChangeStatus_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup User
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup Organization and Actor
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));
        var orgValue = actor.Status;

        // Arrange - Setup Repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IActorAuditLogRepository>();

        // Make an audited change.
        var result = await actorRepository.AddOrUpdateAsync(actor);
        actor = await actorRepository.GetAsync(result.Value);
        actor!.Status = ActorStatus.Active;
        await actorRepository.AddOrUpdateAsync(actor);

        // Act
        var actual = await actorAuditLogEntryRepository
            .GetAsync(result.Value);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs);
        Assert.Contains(actorAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(actorAuditLogs, o => o.Change == ActorAuditedChange.Status);
        Assert.Contains(actorAuditLogs, o => o.CurrentValue == ActorStatus.Active.ToString());
        Assert.Contains(actorAuditLogs, o => o.PreviousValue == orgValue.ToString());
    }

    [Fact]
    public async Task GetAsync_AssignRemoveAssignCredentials_HasCorrectAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        // Arrange - Setup User
        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        // Arrange - Setup Actor
        var actorEntity = await _fixture.PrepareActorAsync();
        var actorId = new ActorId(actorEntity.Id);

        // Arrange - Setup Repositories
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IActorAuditLogRepository>();

        var actorCertificateCredentials = new ActorCertificateCredentials(
            "mocked_print_A",
            "mocked_id",
            SystemClock.Instance.GetCurrentInstant());

        var actorClientSecretCredentials = new ActorClientSecretCredentials(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SystemClock.Instance.GetCurrentInstant());

        // Make an audited change.
        var actor = await actorRepository.GetAsync(actorId);
        actor!.Credentials = actorCertificateCredentials;
        await actorRepository.AddOrUpdateAsync(actor);

        actor.Credentials = null;
        await actorRepository.AddOrUpdateAsync(actor);

        actor.ExternalActorId = new ExternalActorId(actorClientSecretCredentials.ClientId);
        actor.Credentials = actorClientSecretCredentials;
        await actorRepository.AddOrUpdateAsync(actor);

        // Act
        var actual = await actorAuditLogEntryRepository.GetAsync(actorId);

        // Assert
        var actorAuditLogs = actual.ToList();
        Assert.NotEmpty(actorAuditLogs);

        Assert.Contains(actorAuditLogs, entry =>
            entry is { Change: ActorAuditedChange.CertificateCredentials, PreviousValue: null } &&
            entry.CurrentValue == actorCertificateCredentials.CertificateThumbprint);

        Assert.Contains(actorAuditLogs, entry =>
            entry is { Change: ActorAuditedChange.CertificateCredentials, CurrentValue: null } &&
            entry.PreviousValue == actorCertificateCredentials.CertificateThumbprint);

        Assert.Contains(actorAuditLogs, entry =>
            entry is { Change: ActorAuditedChange.ClientSecretCredentials, PreviousValue: null } &&
            entry.CurrentValue == actorClientSecretCredentials.ExpirationDate.ToString("g", CultureInfo.InvariantCulture));
    }
}
