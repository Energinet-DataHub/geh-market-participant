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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class UniqueActorMarketRoleGridAreaRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public UniqueActorMarketRoleGridAreaRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task TryAdd_NoGridAreaAssociatedWithMarketRole_ReturnsTrue()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridArea = await CreateGridAreaAsync(context).ConfigureAwait(false);
            var actor = await CreateActorUnderNewOrganizationAsync(context).ConfigureAwait(false);

            var target = new UniqueActorMarketRoleGridAreaRepository(context);

            // Act
            var actual = await target.TryAddAsync(new UniqueActorMarketRoleGridArea(actor.Id, EicFunction.EnergySupplier, gridArea.Id.Value)).ConfigureAwait(false);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public async Task TryAdd_ExistingGridAreaAssociatedWithMarketRole_ReturnsFalse()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridArea = await CreateGridAreaAsync(context).ConfigureAwait(false);
            var actor = await CreateActorUnderNewOrganizationAsync(context).ConfigureAwait(false);

            var target = new UniqueActorMarketRoleGridAreaRepository(context);
            await target.TryAddAsync(new UniqueActorMarketRoleGridArea(actor.Id, EicFunction.EnergySupplier, gridArea.Id.Value)).ConfigureAwait(false);

            var newActor = await CreateActorUnderNewOrganizationAsync(context).ConfigureAwait(false);

            // Act
            var actual = await target.TryAddAsync(new UniqueActorMarketRoleGridArea(newActor.Id, EicFunction.EnergySupplier, gridArea.Id.Value)).ConfigureAwait(false);

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public async Task Remove_RemovesAssociationsFromActor()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridArea = await CreateGridAreaAsync(context).ConfigureAwait(false);
            var actor = await CreateActorUnderNewOrganizationAsync(context).ConfigureAwait(false);

            var target = new UniqueActorMarketRoleGridAreaRepository(context);

            // Act
            var firstAddResult = await target.TryAddAsync(new UniqueActorMarketRoleGridArea(actor.Id, EicFunction.EnergySupplier, gridArea.Id.Value)).ConfigureAwait(false);
            await target.RemoveAsync(actor.Id).ConfigureAwait(false);
            var secondAddResult = await target.TryAddAsync(new UniqueActorMarketRoleGridArea(actor.Id, EicFunction.EnergySupplier, gridArea.Id.Value)).ConfigureAwait(false);

            // Assert
            Assert.True(firstAddResult);
            Assert.True(secondAddResult);
        }

        private static async Task<Actor> CreateActorUnderNewOrganizationAsync(MarketParticipantDbContext context)
        {
            var organization = new Organization(Guid.NewGuid().ToString(), new BusinessRegisterIdentifier("12345678"), new Address(null, null, null, null, "DK"));
            organization.Actors.Add(new Actor(new MockedGln()));

            var repository = new OrganizationRepository(context);
            var id = await repository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = (await repository.GetAsync(id).ConfigureAwait(false))!;
            return organization.Actors.First();
        }

        private static async Task<GridArea> CreateGridAreaAsync(MarketParticipantDbContext context)
        {
            var name = Guid.NewGuid().ToString();
            var domain = new GridArea(new GridAreaName(name), new GridAreaCode("001"), PriceAreaCode.Dk1);

            var repository = new GridAreaRepository(context);
            var id = await repository.AddOrUpdateAsync(domain).ConfigureAwait(false);
            return (await repository.GetAsync(id).ConfigureAwait(false))!;
        }
    }
}
