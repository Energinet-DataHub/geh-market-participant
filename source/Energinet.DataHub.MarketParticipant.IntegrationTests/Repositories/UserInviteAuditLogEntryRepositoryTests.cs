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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserInviteAuditLogEntryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserInviteAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var userInviteAuditLogEntryRepository = new UserInviteAuditLogEntryRepository(contextGet);

        var userId = new UserId(Guid.NewGuid());
        var actorId = Guid.NewGuid();

        // Act
        var actual = await userInviteAuditLogEntryRepository
            .GetAsync(userId, actorId)
            .ConfigureAwait(false);

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetAsync_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var contextGet = _fixture.DatabaseManager.CreateDbContext();
        var userInviteAuditLogEntryRepository = new UserInviteAuditLogEntryRepository(contextGet);

        var user = await _fixture.PrepareUserAsync();
        var userChangedBy = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync();

        var entry = new UserInviteAuditLogEntry(
             new UserId(user.Id),
             new UserId(userChangedBy.Id),
             actor.Id,
             DateTimeOffset.UtcNow);

        // Insert an audit log.
        await using var contextInsert = _fixture.DatabaseManager.CreateDbContext();

        var insertAuditLogEntryRepository = new UserInviteAuditLogEntryRepository(contextInsert);
        await insertAuditLogEntryRepository.InsertAuditLogEntryAsync(entry);

        // Act
        var actual = await userInviteAuditLogEntryRepository
            .GetAsync(new UserId(user.Id), actor.Id)
            .ConfigureAwait(false);

        // Assert
        Assert.Single(actual, entry);
    }
}
