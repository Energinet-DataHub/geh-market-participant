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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class OrganizationRepository : IOrganizationRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public OrganizationRepository(
            IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<Result<OrganizationId, OrganizationError>> AddOrUpdateAsync(Organization organization)
        {
            ArgumentNullException.ThrowIfNull(organization, nameof(organization));

            OrganizationEntity destination;

            if (organization.Id.Value == default)
            {
                destination = new OrganizationEntity();
            }
            else
            {
                destination = await _marketParticipantDbContext
                    .Organizations
                    .FirstAsync(x => x.Id == organization.Id.Value)
                    .ConfigureAwait(false);
            }

            OrganizationMapper.MapToEntity(organization, destination);
            _marketParticipantDbContext.Organizations.Update(destination);

            try
            {
                await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException inner &&
                inner.Message.Contains("UQ_Organization_Domain", StringComparison.InvariantCultureIgnoreCase))
            {
                return new(OrganizationError.DomainConflict);
            }

            return new(new OrganizationId(destination.Id));
        }

        public async Task<Organization?> GetAsync(OrganizationId id)
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));

            var org = await _marketParticipantDbContext
                .Organizations
                .FirstOrDefaultAsync(x => x.Id == id.Value)
                .ConfigureAwait(false);

            return org is not null ? OrganizationMapper.MapFromEntity(org) : null;
        }

        public async Task<IEnumerable<Organization>> GetAsync()
        {
            var entities = await _marketParticipantDbContext
                .Organizations
                .OrderBy(x => x.Name)
                .ToListAsync()
                .ConfigureAwait(false);

            return entities.Select(OrganizationMapper.MapFromEntity);
        }

        public async Task<IEnumerable<Organization>> GetAsync(ActorNumber actorNumber)
        {
            ArgumentNullException.ThrowIfNull(actorNumber, nameof(actorNumber));

            var query =
                from actor in _marketParticipantDbContext.Actors
                join organization in _marketParticipantDbContext.Organizations
                    on actor.OrganizationId equals organization.Id
                where actor.ActorNumber == actorNumber.Value
                select organization;

            var organizations = await query.ToListAsync().ConfigureAwait(false);
            return organizations.Select(OrganizationMapper.MapFromEntity);
        }
    }
}
