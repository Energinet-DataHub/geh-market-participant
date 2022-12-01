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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Slim;

public sealed class UserRepository : IUserRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<IEnumerable<Guid>> GetActorsAsync(ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(externalUserId);

        var roleAssignmentsQuery = await _marketParticipantDbContext
            .Users
            .Where(u => u.ExternalId == externalUserId.Value)
            .Include(u => u.RoleAssignments)
            .SelectMany(u => u.RoleAssignments)
            .Select(x => x.ActorId)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);

        return roleAssignmentsQuery;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(Guid actorId, ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(externalUserId);

        var perms = await _marketParticipantDbContext
            .Users
            .Where(u => u.ExternalId == externalUserId.Value)
            .Include(u => u.RoleAssignments.Where(r => r.ActorId == actorId))
            .ThenInclude(r => r.UserRoleTemplate)
            .ThenInclude(t => t.Permissions)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);

        return perms.SelectMany(x => x.RoleAssignments.SelectMany(y => y.UserRoleTemplate.Permissions.Select(z => z.Permission)));
    }

    public Task<bool> IsFasAsync(Guid actorId, ExternalUserId externalUserId)
    {
        var query = from u in _marketParticipantDbContext.Users
                    join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
                    join a in _marketParticipantDbContext.Actors on r.ActorId equals a.Id
                    where u.ExternalId == externalUserId.Value
                    select a.IsFas;

        return query.FirstOrDefaultAsync();
    }
}
