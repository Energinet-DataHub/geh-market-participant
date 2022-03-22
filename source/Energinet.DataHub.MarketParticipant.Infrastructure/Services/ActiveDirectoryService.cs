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
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.Extensions.Configuration;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public sealed class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly IConfiguration _configuration;

        public ActiveDirectoryService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<ExternalActorId> EnsureAppRegistrationIdAsync(GlobalLocationNumber gln)
        {
            Guard.ThrowIfNull(gln, nameof(gln));

            // This is a temporary implementation using the actor DB.
            // Will be replaced by Azure AD integration at a later time.
            const string param = "GLN";
            const string query = @"SELECT TOP 1 [Id]
                        FROM  [dbo].[ActorInfo]
                        WHERE [dbo].[IdentificationNumber] = @" + param;

            await using var connection = new SqlConnection(_configuration.GetConnectionString("SQL_MP_DB_CONNECTION_STRING"));
            await connection.OpenAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(query, connection)
            {
                Parameters = { new SqlParameter(param, gln.Value) }
            };

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var record = (IDataRecord)reader;
                return new ExternalActorId(record.GetString(0));
            }

            return new ExternalActorId(Guid.NewGuid());
        }
    }
}
