﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentAssertions.Common;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorConsolidationRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ActorConsolidationRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_ConsolidationNotExists_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var consolidationRepository = new ActorConsolidationRepository(context);

        // Act
        var consolidation = await consolidationRepository
            .GetAsync(new ActorConsolidationId(Guid.NewGuid()));

        // Assert
        Assert.Null(consolidation);
    }

    [Fact]
    public async Task AddAsync_OneActorConsolidation_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var consolidationRepository = new ActorConsolidationRepository(context);
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var consolidationRepository2 = new ActorConsolidationRepository(context2);
        var scheduledAt = DateTimeOffset.Now.Date.AddMonths(2).ToDateTimeOffset().ToInstant();
        var actorFrom = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization.Patch(t => t.Domains.Add(new OrganizationDomainEntity { Domain = "test1.dk" })),
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.EnergySupplier));
        var actorTo = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization.Patch(t => t.Domains.Add(new OrganizationDomainEntity { Domain = "test2.dk" })),
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.EnergySupplier));

        var testConsolidation = new ActorConsolidation(
            new ActorId(actorFrom.Id),
            new ActorId(actorTo.Id),
            scheduledAt);

        // Act
        var consolidationId = await consolidationRepository.AddAsync(testConsolidation);
        var newConsolidation = await consolidationRepository2.GetAsync(consolidationId);

        // Assert
        Assert.NotNull(newConsolidation);
        Assert.NotEqual(Guid.Empty, newConsolidation.Id.Value);
        Assert.Equal(actorFrom.Id, newConsolidation.ActorFromId.Value);
        Assert.Equal(actorTo.Id, newConsolidation.ActorToId.Value);
        Assert.Equal(ActorConsolidationStatus.Pending, newConsolidation.Status);
        Assert.Equal(scheduledAt, newConsolidation.ScheduledAt);
    }
}