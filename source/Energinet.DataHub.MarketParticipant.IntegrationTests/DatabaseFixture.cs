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
using System.Data.SqlClient;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Helpers;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private const string MarketParticipantDbName = "marketparticipant";
        private const string BaseConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Connection Timeout=3";

        private string _connectionString;

        public DatabaseFixture()
        {
            var builder = new SqlConnectionStringBuilder(BaseConnectionString)
            {
                InitialCatalog = MarketParticipantDbName
            };
            _connectionString = builder.ToString();
        }
        public async Task InitializeAsync()
        {
            await using var connection = new SqlConnection(BaseConnectionString);
            await connection.OpenAsync();

            await ExecuteDbCommandAsync(connection, $"CREATE DATABASE [{MarketParticipantDbName}]");
            await ExecuteDbCommandAsync(connection, $"USE [{MarketParticipantDbName}]");

            ApplyInitialMigrations();
        }

        public async Task DisposeAsync()
        {
            await using var connection = new SqlConnection(BaseConnectionString);
            await connection.OpenAsync();

            await ExecuteDbCommandAsync(connection, $"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{MarketParticipantDbName}'");
            await ExecuteDbCommandAsync(connection, "USE [master]");
            await ExecuteDbCommandAsync(connection, $"ALTER DATABASE [{MarketParticipantDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
            await ExecuteDbCommandAsync(connection, "USE [master]");
            await ExecuteDbCommandAsync(connection, $"DROP DATABASE [{MarketParticipantDbName}]");
        }

        private void ApplyInitialMigrations()
        {
            var upgrader = UpgradeFactory.GetUpgradeEngine(_connectionString, GetFilter(), false);
           upgrader.PerformUpgrade();
        }

        private static async Task ExecuteDbCommandAsync(SqlConnection connection, string commandText)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            await cmd.ExecuteNonQueryAsync();
        }

        private Func<string, bool> GetFilter()
        {
           return file =>
               file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
               file.Contains(".Scripts.LocalDB.", StringComparison.OrdinalIgnoreCase);
        }
    }
}
