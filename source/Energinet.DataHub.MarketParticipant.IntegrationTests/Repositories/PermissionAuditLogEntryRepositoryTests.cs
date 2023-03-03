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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class PermissionAuditLogEntryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public PermissionAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var permissionAuditLogEntryRepository = new PermissionAuditLogEntryRepository(contextGet);

        // Act
        var actual = await permissionAuditLogEntryRepository
            .GetAsync(Permission.UserRoleManage)
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
        var permissionAuditLogEntryRepository = new PermissionAuditLogEntryRepository(contextGet);

        var userChangedBy = await _fixture.PrepareUserAsync();
        var userRoleWithCreatedPermission = await _fixture.PrepareUserRoleAsync(Permission.UsersManage);

        var entry = new PermissionAuditLogEntry(
            1,
            userRoleWithCreatedPermission.Permissions[0].Permission,
            new UserId(userChangedBy.Id),
            PermissionChangeType.DescriptionChange,
            DateTimeOffset.UtcNow);

        // Insert an audit log.
        await using var contextInsert = _fixture.DatabaseManager.CreateDbContext();

        var insertAuditLogEntryRepository = new PermissionAuditLogEntryRepository(contextInsert);
        await insertAuditLogEntryRepository.InsertAuditLogEntryAsync(entry);

        // Act
        var actual = await permissionAuditLogEntryRepository
            .GetAsync(Permission.UsersManage)
            .ConfigureAwait(false);

        // Assert
        var permissionAuditLogs = actual.ToList();
        Assert.Single(permissionAuditLogs);
        Assert.Equal(entry.EntryId, permissionAuditLogs[0].EntryId);
        Assert.Equal(entry.ChangedByUserId, permissionAuditLogs[0].ChangedByUserId);
        Assert.Equal(entry.Timestamp, permissionAuditLogs[0].Timestamp);
        Assert.Equal(entry.PermissionChangeType, permissionAuditLogs[0].PermissionChangeType);
        Assert.Equal(entry.Permission, permissionAuditLogs[0].Permission);
    }
}
