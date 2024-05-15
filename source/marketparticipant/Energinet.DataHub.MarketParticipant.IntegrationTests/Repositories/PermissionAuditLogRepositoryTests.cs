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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
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
public sealed class PermissionAuditLogRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public PermissionAuditLogRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_NoChanges_ReturnsClaim()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var contextGet = _fixture.DatabaseManager.CreateDbContext();
        var permissionAuditLogEntryRepository = new PermissionAuditLogRepository(contextGet);

        // Act
        var actual = await permissionAuditLogEntryRepository
            .GetAsync(PermissionId.UserRolesManage);

        // Assert
        var permissionAuditLogs = actual.ToList();
        Assert.Single(permissionAuditLogs);
        Assert.Equal(KnownAuditIdentityProvider.Migration.IdentityId, permissionAuditLogs[0].AuditIdentity);
        Assert.Equal(PermissionAuditedChange.Claim, permissionAuditLogs[0].Change);
    }

    [Fact]
    public async Task GetAsync_WithAuditLogs_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        await using var scope = host.BeginScope();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionAuditLogEntryRepository = new PermissionAuditLogRepository(context);

        // Make an audited change.
        var permission = await permissionRepository.GetAsync(PermissionId.UsersManage);
        Assert.NotNull(permission);

        permission.Description = "New description";
        await permissionRepository.UpdatePermissionAsync(permission);

        // Act
        var actual = await permissionAuditLogEntryRepository
            .GetAsync(PermissionId.UsersManage);

        // Assert
        var permissionAuditLogs = actual.Skip(1).ToList();
        Assert.Single(permissionAuditLogs);
        Assert.Equal(user.Id, permissionAuditLogs[0].AuditIdentity.Value);
        Assert.Equal(PermissionAuditedChange.Description, permissionAuditLogs[0].Change);
    }
}
