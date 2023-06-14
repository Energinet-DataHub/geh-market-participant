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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserIdentityAuditLogEntryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserIdentityAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var userIdentityAuditLogEntryRepository = new UserIdentityAuditLogEntryRepository(contextGet);

        var userId = new UserId(Guid.NewGuid());

        // Act
        var actual = await userIdentityAuditLogEntryRepository
            .GetAsync(userId)
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
        var userIdentityAuditLogEntryRepository = new UserIdentityAuditLogEntryRepository(contextGet);

        var user = await _fixture.PrepareUserAsync();
        var userChangedBy = await _fixture.PrepareUserAsync();

        var entry = new UserIdentityAuditLogEntry(
             new UserId(user.Id),
             new UserId(userChangedBy.Id),
             UserIdentityAuditLogField.FirstName,
             Guid.NewGuid().ToString(),
             Guid.NewGuid().ToString(),
             DateTimeOffset.UtcNow);

        // Insert an audit log.
        await using var contextInsert = _fixture.DatabaseManager.CreateDbContext();

        var insertAuditLogEntryRepository = new UserIdentityAuditLogEntryRepository(contextInsert);
        await insertAuditLogEntryRepository.InsertAuditLogEntryAsync(entry);

        // Act
        var actual = await userIdentityAuditLogEntryRepository
            .GetAsync(new UserId(user.Id))
            .ConfigureAwait(false);

        // Assert
        var userIdentityAuditLogs = actual.ToList();
        Assert.Single(userIdentityAuditLogs);
        Assert.Equal(entry.UserId, userIdentityAuditLogs[0].UserId);
        Assert.Equal(entry.Timestamp, userIdentityAuditLogs[0].Timestamp);
        Assert.Equal(entry.ChangedByUserId, userIdentityAuditLogs[0].ChangedByUserId);
        Assert.Equal(entry.Field, userIdentityAuditLogs[0].Field);
        Assert.Equal(entry.NewValue, userIdentityAuditLogs[0].NewValue);
        Assert.Equal(entry.OldValue, userIdentityAuditLogs[0].OldValue);
    }
}
