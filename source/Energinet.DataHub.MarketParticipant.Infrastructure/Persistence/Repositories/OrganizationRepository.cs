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
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class OrganizationRepository: IOrganizationRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public OrganizationRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<OrganizationId> AddAsync(Organization organization)
        {
            Guard.Against.Null(organization, nameof(organization));
            var orgEntity = OrganizationMapper.MapToEntity(organization);
            await _marketParticipantDbContext.Organizations.AddAsync(orgEntity).ConfigureAwait(false);
            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
            return new OrganizationId(orgEntity.Id);
        }

        public async Task UpdateAsync(Organization organization)
        {
            Guard.Against.Null(organization, nameof(organization));
            _marketParticipantDbContext.Organizations.Update(OrganizationMapper.MapToEntity(organization));
            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<Organization> GetAsync(OrganizationId id)
        {
            return  OrganizationMapper.MapFromEntity(await _marketParticipantDbContext.Organizations.SingleOrDefaultAsync(s => s.Id == id.Value));
        }
    }
}
