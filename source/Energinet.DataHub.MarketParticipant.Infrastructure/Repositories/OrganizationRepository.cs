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

using System.Data.SqlClient;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Dapper;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly ActorDbConfig _actorDbConfig;

        public OrganizationRepository(ActorDbConfig actorDbConfig)
        {
            _actorDbConfig = actorDbConfig;
        }

        public async Task SaveAsync(Organization organization)
        {
            await using var connection = new SqlConnection(_actorDbConfig.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.InsertAsync(organization).ConfigureAwait(false);
        }
    }
}
