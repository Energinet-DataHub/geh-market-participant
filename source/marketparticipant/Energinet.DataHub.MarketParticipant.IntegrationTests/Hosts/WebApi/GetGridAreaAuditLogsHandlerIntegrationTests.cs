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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetGridAreaAuditLogsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetGridAreaAuditLogsHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public Task GetAuditLogs_Decommissioned_IsAudited()
    {
        var expected = DateTimeOffset.UtcNow;

        return TestAuditOfGridAreaChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.Change == GridAreaAuditedChange.Decommissioned && !string.IsNullOrEmpty(log.CurrentValue));

                Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), expectedLog.CurrentValue);
            },
            (gridArea, _) =>
            {
                gridArea.ValidTo = expected;
                return Task.CompletedTask;
            });
    }

    [Fact]
    public Task GetAuditLogs_ConsolidationRequested_IsAudited()
    {
        var expectedFrom = new ActorId(Guid.NewGuid());
        var expectedTo = new ActorId(Guid.NewGuid());
        var consolidateAt = SystemClock.Instance.GetCurrentInstant();

        return TestAuditOfGridAreaChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.Change == GridAreaAuditedChange.ConsolidationRequested && !string.IsNullOrEmpty(log.CurrentValue));

                var expectedCurrentValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(expectedLog.CurrentValue!);
                var expectedPreviousValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(expectedLog.PreviousValue!);
                Assert.Equal(expectedTo.Value, expectedCurrentValue!.ActorId);
                Assert.Equal(consolidateAt.ToDateTimeOffset(), expectedCurrentValue.ConsolidateAt);
                Assert.Equal(expectedFrom.Value, expectedPreviousValue!.ActorId);
                Assert.Equal(consolidateAt.ToDateTimeOffset(), expectedPreviousValue.ConsolidateAt);
            },
            async (gridArea, sp) =>
            {
                var frontendUser = sp.GetRequiredService<IUserContext<FrontendUser>>();
                var actorConsolidationAuditLogRepository = sp.GetRequiredService<IActorConsolidationAuditLogRepository>();

                var actorConsolidation = new ActorConsolidation(
                    expectedFrom,
                    expectedTo,
                    consolidateAt);

                await actorConsolidationAuditLogRepository.AuditAsync(
                    new AuditIdentity(frontendUser.CurrentUser.UserId),
                    GridAreaAuditedChange.ConsolidationRequested,
                    actorConsolidation,
                    gridArea.Id);
            });
    }

    [Fact]
    public Task GetAuditLogs_ConsolidationCompleted_IsAudited()
    {
        var expectedFrom = new ActorId(Guid.NewGuid());
        var expectedTo = new ActorId(Guid.NewGuid());

        return TestAuditOfGridAreaChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.Change == GridAreaAuditedChange.ConsolidationCompleted && !string.IsNullOrEmpty(log.CurrentValue));

                var expectedCurrentValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(expectedLog.CurrentValue!);
                var expectedPreviousValue = JsonSerializer.Deserialize<ActorConsolidationActorAndDate>(expectedLog.PreviousValue!);
                Assert.Equal(expectedTo.ToString(), expectedCurrentValue!.ActorId.ToString());
                Assert.Equal(expectedFrom.ToString(), expectedPreviousValue!.ActorId.ToString());
            },
            async (gridArea, sp) =>
            {
                var frontendUser = sp.GetRequiredService<IUserContext<FrontendUser>>();
                var actorConsolidationAuditLogRepository = sp.GetRequiredService<IActorConsolidationAuditLogRepository>();

                var actorConsolidation = new ActorConsolidation(
                    expectedFrom,
                    expectedTo,
                    SystemClock.Instance.GetCurrentInstant());

                await actorConsolidationAuditLogRepository.AuditAsync(
                    new AuditIdentity(frontendUser.CurrentUser.UserId),
                    GridAreaAuditedChange.ConsolidationCompleted,
                    actorConsolidation,
                    gridArea.Id);
            });
    }

    private async Task TestAuditOfGridAreaChangeAsync(
        Action<GetGridAreaAuditLogsResponse> assert,
        params Func<GridArea, IServiceProvider, Task>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();
        userContext
            .Setup(uc => uc.CurrentUser)
            .Returns(new FrontendUser(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false));

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var gridAreaRepository = scope.ServiceProvider.GetRequiredService<IGridAreaRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var baseGridArea = new GridArea(
            new GridAreaName("fake_value"),
            new GridAreaCode(Random.Shared.Next().ToString(CultureInfo.InvariantCulture)[..3]),
            PriceAreaCode.Dk1,
            GridAreaType.Distribution,
            DateTimeOffset.MinValue,
            null);

        var createdGridArea = await gridAreaRepository.AddOrUpdateAsync(baseGridArea).ConfigureAwait(false);

        var command = new GetGridAreaAuditLogsCommand(createdGridArea.Value);
        var auditLogsProcessed = 0;

        foreach (var action in changeActions)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var gridArea = await gridAreaRepository.GetAsync(new GridAreaId(createdGridArea.Value));
            Assert.NotNull(gridArea);

            await action(gridArea, scope.ServiceProvider);
            await gridAreaRepository.AddOrUpdateAsync(gridArea);

            var auditLogs = await mediator.Send(command);

            foreach (var actorAuditLog in auditLogs.AuditLogs.Skip(auditLogsProcessed))
            {
                Assert.Equal(auditedUser.Id, actorAuditLog.AuditIdentityId);
                Assert.True(actorAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(actorAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));

                auditLogsProcessed++;
            }
        }

        // Act
        var actual = await mediator.Send(command);

        await using var dbContext = _databaseFixture.DatabaseManager.CreateDbContext();
        await dbContext.ActorConsolidationAuditLogEntries.Where(log => log.GridAreaId == createdGridArea.Value).ExecuteDeleteAsync();
        await dbContext.GridAreas.Where(ga => ga.Id == createdGridArea.Value).ExecuteDeleteAsync();

        // Assert
        assert(actual);
    }
}
