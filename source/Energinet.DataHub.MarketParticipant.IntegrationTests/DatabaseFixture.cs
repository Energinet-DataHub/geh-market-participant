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
using System.Data.SqlClient;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Helpers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private const string MarketParticipantDbName = "marketparticipant";
        private const string BaseConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Connection Timeout=3";
        private readonly string _connectionString;

        public DatabaseFixture()
        {
            var builder = new SqlConnectionStringBuilder(BaseConnectionString)
            {
                InitialCatalog = MarketParticipantDbName
            };
            _connectionString = builder.ToString();
        }

        public MarketParticipantDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<MarketParticipantDbContext>()
                .UseSqlServer(_connectionString);

            return new MarketParticipantDbContext(optionsBuilder.Options);
        }

        public async Task InitializeAsync()
        {
            await using var connection = new SqlConnection(BaseConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await ExecuteDbCommandAsync(connection, $"CREATE DATABASE [{MarketParticipantDbName}]").ConfigureAwait(false);
            await ExecuteDbCommandAsync(connection, $"USE [{MarketParticipantDbName}]").ConfigureAwait(false);

            ApplyInitialMigrations();
        }

        public async Task DisposeAsync()
        {
            await using var connection = new SqlConnection(BaseConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await ExecuteDbCommandAsync(
                    connection,
                    $"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{MarketParticipantDbName}'")
                .ConfigureAwait(false);

            await ExecuteDbCommandAsync(
                    connection,
                    "USE [master]")
                .ConfigureAwait(false);

            await ExecuteDbCommandAsync(
                    connection,
                    $"ALTER DATABASE [{MarketParticipantDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE")
                .ConfigureAwait(false);

            await ExecuteDbCommandAsync(
                    connection,
                    "USE [master]")
                .ConfigureAwait(false);

            await ExecuteDbCommandAsync(
                    connection,
                    $"DROP DATABASE [{MarketParticipantDbName}]")
                .ConfigureAwait(false);
        }

        private static Func<string, bool> GetFilter()
        {
            return file =>
                file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
                file.Contains(".Scripts.LocalDB.", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task ExecuteDbCommandAsync(SqlConnection connection, string commandText)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private void ApplyInitialMigrations()
        {
            var upgradeEngine = UpgradeFactory.GetUpgradeEngine(_connectionString, GetFilter());
            upgradeEngine.PerformUpgrade();
        }
    }
}
