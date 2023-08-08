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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ActorRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneActor_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context);
        var actorRepository2 = new ActorRepository(context2);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln());

        // Act
        var actorId = await actorRepository.AddOrUpdateAsync(actor);
        var actual = await actorRepository2.GetAsync(actorId);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ActorWithMarkedRolesAndGridAreas_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context);
        var actorRepository2 = new ActorRepository(context2);
        var gridAreaRepository = new GridAreaRepository(context2);

        var gridAreaId = await gridAreaRepository.AddOrUpdateAsync(new GridArea(
            new GridAreaName("fake_value"),
            new GridAreaCode("000"),
            PriceAreaCode.Dk1,
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue));

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln());

        actor.MarketRoles.Add(new ActorMarketRole(EicFunction.BalanceResponsibleParty, new[]
        {
            new ActorGridArea(gridAreaId, new[] { MeteringPointType.D01VeProduction })
        }));

        // Act
        var actorId = await actorRepository.AddOrUpdateAsync(actor);
        var actual = await actorRepository2.GetAsync(actorId);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
        Assert.Equal(actor.MarketRoles.Single().Function, actual.MarketRoles.Single().Function);
    }

    [Fact]
    public async Task GetActorsAsync_All_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context);

        var actor1 = await _fixture.PrepareActorAsync();
        var actor2 = await _fixture.PrepareActorAsync();

        // Act
        var actual = (await actorRepository.GetActorsAsync()).ToList();

        // Assert
        Assert.NotEmpty(actual);
        Assert.Contains(actual, a => a.Id.Value == actor1.Id);
        Assert.Contains(actual, a => a.Id.Value == actor2.Id);
    }

    [Fact]
    public async Task GetActorsAsync_ById_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context);

        var actor1 = await _fixture.PrepareActorAsync();
        var actor2 = await _fixture.PrepareActorAsync();

        // Act
        var actual = await actorRepository.GetActorsAsync(new[] { new ActorId(actor1.Id), new ActorId(actor2.Id) });

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(2, actual.Count());
    }

    [Fact]
    public async Task GetActorsAsync_ForOrganization_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context);

        var actor1 = await _fixture.PrepareActorAsync();
        await _fixture.PrepareActorAsync();

        // Act
        var actual = await actorRepository.GetActorsAsync(new OrganizationId(actor1.OrganizationId));

        // Assert
        Assert.NotNull(actual);
        Assert.Single(actual);
    }
}
