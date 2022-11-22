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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public UserRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(Guid externalActorId, Guid externalUserId)
        {
            ArgumentNullException.ThrowIfNull(externalActorId);
            ArgumentNullException.ThrowIfNull(externalUserId);

            var actorId = await _marketParticipantDbContext
                .Actors
                .Where(x => x.ActorId == externalActorId)
                .Select(x => x.Id)
                .SingleOrDefaultAsync().ConfigureAwait(false);

            var perms = await _marketParticipantDbContext
                .Users
                .Where(u => u.ExternalId == externalUserId)
                .Include(u => u.RoleAssigments.Where(r => r.ActorId == actorId))
                .ThenInclude(r => r.UserRoleTemplate)
                .ThenInclude(t => t.Permissions)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
            return perms.SelectMany(x => x.RoleAssigments.SelectMany(y => y.UserRoleTemplate.Permissions.Select(z => z.Permission)));
        }

        public async Task<IEnumerable<Permission>> GetPermissionsWithJoinsAsync(Guid externalActorId, Guid externalUserId)
        {
            var actorId = await _marketParticipantDbContext
                .Actors
                .Where(x => x.ActorId == externalActorId)
                .Select(x => x.Id)
                .SingleOrDefaultAsync().ConfigureAwait(false);

            var test = await _marketParticipantDbContext
                .Users
                .Where(u => u.ExternalId == externalUserId)
                .Select(u =>
                    new
                    {
                        User = u,
                        RoleAssignments = u.RoleAssigments
                            .Where(r => r.ActorId == actorId)
                            .Join(
                                _marketParticipantDbContext.UserRoleTemplates
                                    .Include(t => t.Permissions),
                                assignment => assignment.UserRoleTemplateId,
                                template => template.Id,
                                (assignment, template) => new { Assignment = assignment, Template = template })
                            .ToList(),
                    })
                .ToListAsync()
                .ConfigureAwait(false);
            return new List<Permission>();
        }
    }
}
