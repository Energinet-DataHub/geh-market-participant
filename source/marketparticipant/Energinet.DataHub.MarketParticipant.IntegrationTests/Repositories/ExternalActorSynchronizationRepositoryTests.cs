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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(SyncIntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ExternalActorSynchronizationRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ExternalActorSynchronizationRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NextAsync_NoSync_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IExternalActorSynchronizationRepository>();

        // Act
        var next = await target.NextAsync();

        // Assert
        Assert.Null(next);
    }

    [Fact]
    public async Task NextAsync_OneSync_IsReturned()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IExternalActorSynchronizationRepository>();

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);

        // Act
        var next = await target.NextAsync();

        actor.ActorId = null;
        actor.Status = ActorStatus.Inactive;

        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        dbContext.Actors.Update(actor);
        await dbContext.SaveChangesAsync();

        // Assert
        Assert.NotNull(next);
        Assert.Equal(actor.Id, next);
    }

    [Fact]
    public async Task NextAsync_TillEmpty_ReturnsNull()
    {
        // Arrange
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IExternalActorSynchronizationRepository>();

        // Act
        for (var i = 0; i < 4; i++)
        {
            var actor = await _fixture.PrepareActorAsync(
                TestPreparationEntities.ValidOrganization,
                TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Active),
                TestPreparationEntities.ValidMarketRole);

            var next = await target.NextAsync();
            Assert.Equal(actor.Id, next);

            actor.ActorId = null;
            actor.Status = ActorStatus.Inactive;

            dbContext.Actors.Update(actor);
            await dbContext.SaveChangesAsync();
        }

        var last = await target.NextAsync();

        // Assert
        Assert.Null(last);
    }

    [Theory]
    [InlineData("960E8F7B-7351-4B6A-B60E-E84EB358ABDC", ActorStatus.New, true)]
    [InlineData("960E8F7B-7351-4B6A-B60E-E84EB358ABDC", ActorStatus.Active, false)]
    [InlineData("960E8F7B-7351-4B6A-B60E-E84EB358ABDC", ActorStatus.Passive, false)]
    [InlineData("960E8F7B-7351-4B6A-B60E-E84EB358ABDC", ActorStatus.Inactive, true)]
    [InlineData(null, ActorStatus.New, false)]
    [InlineData(null, ActorStatus.Active, true)]
    [InlineData(null, ActorStatus.Passive, true)]
    [InlineData(null, ActorStatus.Inactive, false)]
    public async Task NextAsync_ActorStatus_IsSynchronized(string? externalId, ActorStatus actorStatus, bool shouldSync)
    {
        // Arrange
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IExternalActorSynchronizationRepository>();

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a =>
            {
                a.ActorId = externalId != null ? Guid.Parse(externalId) : null;
                a.Status = actorStatus;
            }),
            TestPreparationEntities.ValidMarketRole);

        // Act
        var next = await target.NextAsync();

        // Assert
        if (shouldSync)
        {
            Assert.Equal(actor.Id, next);
        }
        else
        {
            Assert.Null(next);
        }

        actor.ActorId = null;
        actor.Status = ActorStatus.Inactive;

        dbContext.Actors.Update(actor);
        await dbContext.SaveChangesAsync();
    }
}
