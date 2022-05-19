// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
using System;
using System.Linq;
using Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Helpers;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Migration;

[Collection("IntegrationTest")]
[IntegrationTest]
public class MigrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    public MigrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void TestMigrationFlow()
    {
        // Arrange
        var args = new string[]
        {
            _fixture.DatabaseManager.ConnectionString,
            "LocalDb",
            "dryRun"
        };
        var connectionString = ConnectionStringFactory.GetConnectionString(args);
        var isDryRun = args.Contains("dryRun");

        // Act
        var upgrader = UpgradeFactory.GetUpgradeEngine(connectionString, GetFilter(), isDryRun);

        // Assert
        Assert.True(upgrader.TryConnect(out _));
        Assert.True(upgrader.GetDiscoveredScripts().Any());
    }

    private static Func<string, bool> GetFilter()
    {
        return file =>
            file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
            file.Contains(".Scripts.LocalDB.", StringComparison.OrdinalIgnoreCase);
    }
}
