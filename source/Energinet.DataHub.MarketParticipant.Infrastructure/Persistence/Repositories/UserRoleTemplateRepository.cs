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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class UserRoleTemplateRepository : IUserRoleTemplateRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public UserRoleTemplateRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<Guid> AddOrUpdateAsync(UserRoleTemplate userRoleTemplate)
        {
            ArgumentNullException.ThrowIfNull(userRoleTemplate);

            UserRoleTemplateEntity destination;
            if (userRoleTemplate.Id == Guid.Empty)
            {
                destination = new UserRoleTemplateEntity();
            }
            else
            {
                destination = await GetQuery()
                    .FirstAsync(x => x.Id == userRoleTemplate.Id)
                    .ConfigureAwait(false);
            }

            UserRoleTemplateMapper.MapToEntity(userRoleTemplate, destination);
            _marketParticipantDbContext.UserRoleTemplates.Update(destination);

            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);

            return destination.Id;
        }

        public async Task<UserRoleTemplate?> GetAsync(Guid id)
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));

            var userRoleTemplate = await GetQuery()
                .FirstOrDefaultAsync(x => x.Id == id)
                .ConfigureAwait(false);

            return userRoleTemplate is null ? null : null; // UserRoleTemplateMapper.MapFromEntity(userRoleTemplate);
        }

        public async Task<IEnumerable<UserRoleTemplate>> GetAsync()
        {
            var result = await GetQuery()
                .OrderBy(x => x.Name)
                .ToListAsync()
                .ConfigureAwait(false);
            return new List<UserRoleTemplate>(); // result.Select(UserRoleTemplateMapper.MapFromEntity);
        }

        public async Task<IEnumerable<UserRoleTemplate>> GetForMarketAsync(EicFunction marketRole)
        {
            var result = await GetQuery()
                .Join(
                    _marketParticipantDbContext.MarketRoleToUserRoleTemplate,
                    o => o.Id,
                    i => i.UserRoleTemplateId,
                    (o, i) => new { UserRoleTemplate = o, i.Function })
                .Where(x => x.Function == marketRole)
                .OrderBy(x => x.UserRoleTemplate.Name)
                .ToListAsync()
                .ConfigureAwait(false);
            return new List<UserRoleTemplate>(); // result.Select(x => UserRoleTemplateMapper.MapFromEntity(x.UserRoleTemplate));
        }

        private IQueryable<UserRoleTemplateEntity> GetQuery()
        {
            return _marketParticipantDbContext
                .UserRoleTemplates
                .Include(x => x.Permissions)
                .AsSingleQuery();
        }
    }
}
