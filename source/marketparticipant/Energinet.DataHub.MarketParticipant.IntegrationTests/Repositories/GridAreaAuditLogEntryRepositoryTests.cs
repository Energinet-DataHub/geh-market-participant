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
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class GridAreaAuditLogEntryRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public GridAreaAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
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

            var gridArea = new GridArea(
                new GridAreaName("name"),
                new GridAreaCode("100"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            var gridAreaRepository = new GridAreaRepository(context);
            var gridAreaId = await gridAreaRepository.AddOrUpdateAsync(gridArea);

            var changedGridArea = new GridArea(
                gridAreaId,
                new GridAreaName("different name"),
                new GridAreaCode("100"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            await gridAreaRepository.AddOrUpdateAsync(changedGridArea);

            var target = new GridAreaAuditLogEntryRepository(context);

            // Act
            var actual = (await target.GetAsync(gridAreaId)).ToList();

            // Assert
            Assert.Single(actual);
            Assert.Equal(gridAreaId, actual.Single().GridAreaId);
        }
    }
}
