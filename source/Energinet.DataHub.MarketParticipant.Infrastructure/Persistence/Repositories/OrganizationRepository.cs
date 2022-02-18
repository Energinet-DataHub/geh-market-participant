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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public OrganizationRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<OrganizationId> AddOrUpdateAsync(Organization organization)
        {
            Guard.ThrowIfNull(organization, nameof(organization));

            OrganizationEntity source;

            if (organization.Id.Value == default)
            {
                source = new OrganizationEntity();
            }
            else
            {
                source = await _marketParticipantDbContext
                    .Organizations
                    .FirstAsync(entity => entity.Id == organization.Id.Value)
                    .ConfigureAwait(false);
            }

            OrganizationMapper.MapToEntity(organization, source);
            _marketParticipantDbContext.Organizations.Update(source);

            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
            return new OrganizationId(source.Id);
        }

        public async Task<Organization?> GetAsync(OrganizationId id)
        {
            var org = await _marketParticipantDbContext
                .Organizations
                .FirstOrDefaultAsync(s => s.Id == id.Value)
                .ConfigureAwait(false);

            return org is not null ? OrganizationMapper.MapFromEntity(org) : null;
        }
    }
}
