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
using Dapper;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using DapperExtensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Repositories
{
    public sealed class OrganizationRepository : IOrganizationRepository
    {
        private readonly ActorDbConfig _actorDbConfig;

        public OrganizationRepository(ActorDbConfig actorDbConfig)
        {
            _actorDbConfig = actorDbConfig;
        }

        public async Task<Uuid> AddAsync(Organization organization)
        {
            await using var connection = new SqlConnection(_actorDbConfig.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var orgToAdd = new OrganizationEntity {Gln = organization.Gln.Value, Id = organization.Id.AsGuid(), Name = organization.Name};
            return new Uuid(await connection.InsertAsync(orgToAdd).ConfigureAwait(false));
        }

        public async Task UpdateAsync(Organization organization)
        {
            await using var connection = new SqlConnection(_actorDbConfig.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.UpdateAsync(organization).ConfigureAwait(false);
        }

        public async Task<Organization> GetAsync(Uuid id)
        {
            await using var connection = new SqlConnection(_actorDbConfig.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var orgEnt = await connection.GetAsync<OrganizationEntity>(id.AsGuid());
            return new Organization(new Uuid(orgEnt.Id), new GlobalLocationNumber(orgEnt.Gln), orgEnt.Name);
            // return await connection.QuerySingleAsync<Organization>("SELECT * FROM OrganizationInfo WHERE Id = @id",
            //     new {Id = id } ).ConfigureAwait(false);
        }
    }
}
