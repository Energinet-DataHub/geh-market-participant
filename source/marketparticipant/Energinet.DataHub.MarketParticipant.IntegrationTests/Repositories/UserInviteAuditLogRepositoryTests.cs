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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UserInviteAuditLogRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserInviteAuditLogRepositoryTests(MarketParticipantDatabaseFixture fixture)
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
        var userInviteAuditLogRepository = new UserInviteAuditLogRepository(contextGet);

        var userId = new UserId(Guid.NewGuid());

        // Act
        var actual = await userInviteAuditLogRepository
            .GetAsync(userId);

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
        var userInviteAuditLogRepository = new UserInviteAuditLogRepository(contextGet);

        var user = await _fixture.PrepareUserAsync();
        var userChangedBy = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync();

        // Insert an audit log.
        await using var contextInsert = _fixture.DatabaseManager.CreateDbContext();

        var insertAuditLogRepository = new UserInviteAuditLogRepository(contextInsert);
        await insertAuditLogRepository.AuditAsync(
            new UserId(user.Id),
            new AuditIdentity(userChangedBy.Id),
            new ActorId(actor.Id));

        // Act
        var actual = await userInviteAuditLogRepository
            .GetAsync(new UserId(user.Id));

        // Assert
        var userInviteDetailsAuditLogs = actual.ToList();
        Assert.Single(userInviteDetailsAuditLogs);
        Assert.Equal(actor.Id.ToString(), userInviteDetailsAuditLogs[0].CurrentValue);
        Assert.True(userInviteDetailsAuditLogs[0].Timestamp.ToDateTimeOffset() > DateTimeOffset.UtcNow.AddSeconds(-5));
        Assert.True(userInviteDetailsAuditLogs[0].Timestamp.ToDateTimeOffset() < DateTimeOffset.UtcNow.AddSeconds(5));
        Assert.Equal(userChangedBy.Id, userInviteDetailsAuditLogs[0].AuditIdentity.Value);
    }
}
