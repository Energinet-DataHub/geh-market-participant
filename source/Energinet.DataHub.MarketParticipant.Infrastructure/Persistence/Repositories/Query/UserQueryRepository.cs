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
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;

public sealed class UserQueryRepository : IUserQueryRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserQueryRepository(IMarketParticipantDbContext marketParticipantDbContext)
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

        if (roleAssignmentsQuery.Any())
            return roleAssignmentsQuery;

        var fasActorQuery = await _marketParticipantDbContext
            .Actors
            .Where(a => a.IsFas && a.ActorId != null)
            .Select(a => a.Id)
            .Take(1)
            .ToListAsync()
            .ConfigureAwait(false);

        return fasActorQuery;
    }

    // TODO: Add UT for inactive actor.
    public async Task<IEnumerable<Permission>> GetPermissionsAsync(Guid actorId, ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(externalUserId);

        var actorEicFunctions = _marketParticipantDbContext
            .Actors
            .Where(a => a.Id == actorId)
            .Include(a => a.MarketRoles)
            .SelectMany(a => a.MarketRoles)
            .Select(r => r.Function);

        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            join ur in _marketParticipantDbContext.UserRoles on r.UserRoleId equals ur.Id
            where u.ExternalId == externalUserId.Value &&
                  r.ActorId == actorId &&
                  ur.EicFunctions.All(q => actorEicFunctions.Contains(q.EicFunction))
            select ur.Permissions;

        return await query
            .SelectMany(x => x.Select(y => y.Permission))
            .ToListAsync()
            .ConfigureAwait(false);
    }

    // TODO: Add UT for inactive actor.
    public Task<bool> IsFasAsync(Guid actorId, ExternalUserId externalUserId)
    {
        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            join a in _marketParticipantDbContext.Actors on r.ActorId equals a.Id
            where u.ExternalId == externalUserId.Value && a.Id == actorId
            select a.IsFas;

        return query.FirstOrDefaultAsync();
    }
}
