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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class MarketRoleAndGridAreaForActorReservationServiceTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public MarketRoleAndGridAreaForActorReservationServiceTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task TryAdd_NoGridAreaAssociatedWithMarketRole_ReturnsTrue()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridArea = await CreateGridAreaAsync(context);
            var actor = await CreateActorUnderNewOrganizationAsync(_fixture, context);

            var target = new MarketRoleAndGridAreaForActorReservationService(context);

            // Act
            var actual = await target.TryReserveAsync(actor.Id, EicFunction.EnergySupplier, gridArea.Id);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public async Task TryAdd_ExistingGridAreaAssociatedWithMarketRole_ReturnsFalse()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridArea = await CreateGridAreaAsync(context);
            var actor = await CreateActorUnderNewOrganizationAsync(_fixture, context);

            var target = new MarketRoleAndGridAreaForActorReservationService(context);
            await target.TryReserveAsync(actor.Id, EicFunction.EnergySupplier, gridArea.Id);

            var newActor = await CreateActorUnderNewOrganizationAsync(_fixture, context);

            // Act
            var actual = await target.TryReserveAsync(newActor.Id, EicFunction.EnergySupplier, gridArea.Id);

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public async Task Remove_RemovesAssociationsFromActor()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var gridArea = await CreateGridAreaAsync(context);
            var actor = await CreateActorUnderNewOrganizationAsync(_fixture, context);

            var target = new MarketRoleAndGridAreaForActorReservationService(context);

            // Act
            var firstAddResult = await target.TryReserveAsync(actor.Id, EicFunction.EnergySupplier, gridArea.Id);
            var secondAddResult = await target.TryReserveAsync(actor.Id, EicFunction.BalanceResponsibleParty, gridArea.Id);
            await target.RemoveAllReservationsAsync(actor.Id);
            await target.TryReserveAsync(actor.Id, EicFunction.EnergySupplier, gridArea.Id);
            await target.TryReserveAsync(actor.Id, EicFunction.BalanceResponsibleParty, gridArea.Id);

            // Assert
            Assert.True(firstAddResult);
            Assert.True(secondAddResult);
        }

        private static async Task<Actor> CreateActorUnderNewOrganizationAsync(MarketParticipantDatabaseFixture fixture, MarketParticipantDbContext context)
        {
            var actor = await fixture.PrepareActorAsync();
            var repository = new ActorRepository(context);
            return (await repository.GetAsync(new ActorId(actor.Id)))!;
        }

        private static async Task<GridArea> CreateGridAreaAsync(MarketParticipantDbContext context)
        {
            var domain = new GridArea(
                new GridAreaName("fake_value"),
                new GridAreaCode("123"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            var repository = new GridAreaRepository(context);
            var id = await repository.AddOrUpdateAsync(domain);
            return (await repository.GetAsync(id))!;
        }
    }
}
