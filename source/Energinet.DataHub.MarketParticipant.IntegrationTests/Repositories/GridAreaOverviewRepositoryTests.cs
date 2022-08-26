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

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class GridAreaOverviewRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public GridAreaOverviewRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAsync_ReturnsGridAreas()
        {
            // arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var target = new GridAreaOverviewRepository(context);

            // act
            var actual = (await target
                .GetAsync()
                .ConfigureAwait(false)).ToList();

            // assert
            Assert.NotEmpty(actual);
        }

        [Fact]
        public async Task GetAsync_ActorMarketRoleAssigne_ReturnsWithActorNameAndNumber()
        {
            // arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var (gridAreaId, actorNumber, actorName) = await CreateGridAreaAssignedToGridAccessProvider();
            var gridAreaIdNotAssignedToActor = await CreateGridArea();

            var target = new GridAreaOverviewRepository(context);

            // act
            var actual = (await target
                .GetAsync()
                .ConfigureAwait(false)).ToList();

            // assert
            Assert.NotNull(actual.Single(x => x.Id == gridAreaId && x.ActorName == actorName && x.ActorNumber == actorNumber));
            Assert.NotNull(actual.Single(x => x.Id == gridAreaIdNotAssignedToActor));
        }

        private async Task<(GridAreaId GridAreaId, ActorNumber ActorNumber, ActorName ActorName)> CreateGridAreaAssignedToGridAccessProvider()
        {
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var organizationId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var marketRoleId = Guid.NewGuid();
            var marketRoleGridAreaId = Guid.NewGuid();

            var gridAreaRepo = new GridAreaRepository(context);
            var gridAreaId = await CreateGridArea();

            var orgRepo = new OrganizationRepository(context);
            var org = new Organization("name", MockedBusinessRegisterIdentifier.New(), new Address(null, null, null, null, "DK"));

            var actor = new Actor(new MockedGln());
            actor.MarketRoles.Add(new ActorMarketRole(marketRoleId, EicFunction.GridAccessProvider, new[] { new ActorGridArea(gridAreaId.Value, new[] { MeteringPointType.D01VeProduction }) }));

            org.Actors.Add(actor);

            await orgRepo.AddOrUpdateAsync(org);

            return (gridAreaId, actor.ActorNumber, actor.Name);
        }

        private async Task<GridAreaId> CreateGridArea()
        {
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridAreaRepo = new GridAreaRepository(context);
            return await gridAreaRepo.AddOrUpdateAsync(new GridArea(new GridAreaName("name"), new GridAreaCode("1234"), PriceAreaCode.Dk1));
        }
    }
}
