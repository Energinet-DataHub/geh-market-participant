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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.HealthCheck
{
    public sealed class SqlDatabaseVerifier : ISqlDatabaseVerifier
    {
        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Issue: https://github.com/dotnet/roslyn-analyzers/issues/5712")]
        public async Task<bool> VerifyAsync(string connectionString)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                return false;
            }
        }
    }
}
