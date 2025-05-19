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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Domain;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Authorization;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GridAreaRepositoryTests
{
    private readonly AuthorizationDatabaseFixture _fixture;

    public GridAreaRepositoryTests(AuthorizationDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_GridNotExists_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var gridAreaRepository = new GridAreaRepository(context);

        // Act
        var testOrg = await gridAreaRepository
            .GetAsync(new GridAreaId(Guid.NewGuid()));

        // Assert
        Assert.Null(testOrg);
    }
}
