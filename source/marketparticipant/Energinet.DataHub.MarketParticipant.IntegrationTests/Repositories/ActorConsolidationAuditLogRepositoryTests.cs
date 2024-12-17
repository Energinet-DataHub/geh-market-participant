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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorConsolidationAuditLogRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ActorConsolidationAuditLogRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_GridAreaIdProvided_ReturnsLogEntriesForGridArea()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var gridArea = await _fixture.PrepareGridAreaAsync();
        var gridAreaId = new GridAreaId(gridArea.Id);

        var auditIdentity = new AuditIdentity(Guid.NewGuid());
        var actorConsolidation = new ActorConsolidation(
            new ActorId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            SystemClock.Instance.GetCurrentInstant());

        var target = new ActorConsolidationAuditLogRepository(context);
        await target.AuditAsync(
            auditIdentity,
            GridAreaAuditedChange.ConsolidationRequested,
            actorConsolidation,
            gridAreaId);

        // Act
        var actual = (await target.GetAsync(gridAreaId)).Single();

        // Assert
        var actualCurrentValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(actual.CurrentValue);
        var actualPreviousValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(actual.PreviousValue);
        Assert.Equal(auditIdentity, actual.AuditIdentity);
        Assert.Equal(GridAreaAuditedChange.ConsolidationRequested, actual.Change);
        Assert.Equal(actorConsolidation.ActorFromId.ToString(), actualPreviousValue!.ActorId.ToString());
        Assert.Equal(actorConsolidation.ActorToId.ToString(), actualCurrentValue!.ActorId.ToString());
        Assert.Equal(actorConsolidation.ConsolidateAt.ToDateTimeOffset(), actualCurrentValue.ConsolidateAt);
    }

    [Fact]
    public async Task Get_ActorIdProvided_ReturnsLogEntriesForActor()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var gridArea = await _fixture.PrepareGridAreaAsync();
        var gridAreaId = new GridAreaId(gridArea.Id);

        var auditIdentity = new AuditIdentity(Guid.NewGuid());
        var actorConsolidation = new ActorConsolidation(
            new ActorId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            SystemClock.Instance.GetCurrentInstant());

        var target = new ActorConsolidationAuditLogRepository(context);
        await target.AuditAsync(
            auditIdentity,
            GridAreaAuditedChange.ConsolidationRequested,
            actorConsolidation,
            gridAreaId);

        // Act
        var actual = (await target.GetAsync(actorConsolidation.ActorFromId)).Single();

        // Assert
        var actualCurrentValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(actual.CurrentValue);
        var actualPreviousValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(actual.PreviousValue);
        Assert.Equal(auditIdentity, actual.AuditIdentity);
        Assert.Equal(ActorAuditedChange.ConsolidationRequested, actual.Change);
        Assert.Equal(actorConsolidation.ActorFromId.ToString(), actualPreviousValue!.ActorId.ToString());
        Assert.Equal(actorConsolidation.ActorToId.ToString(), actualCurrentValue!.ActorId.ToString());
        Assert.Equal(actorConsolidation.ConsolidateAt.ToDateTimeOffset(), actualCurrentValue.ConsolidateAt);
    }
}
