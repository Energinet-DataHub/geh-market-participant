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
using Energinet.DataHub.MarketParticipant.Domain.Model;
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

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(Guid actorId, ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(externalUserId);

        var query =
            from user in _marketParticipantDbContext.Users
            where user.ExternalId == externalUserId.Value
            join userRoleAssignment in _marketParticipantDbContext.UserRoleAssignments on user.Id equals userRoleAssignment.UserId
            where userRoleAssignment.ActorId == actorId
            join userRole in _marketParticipantDbContext.UserRoles on userRoleAssignment.UserRoleId equals userRole.Id
            where userRole.Status == UserRoleStatus.Active
            join actor in _marketParticipantDbContext.Actors on userRoleAssignment.ActorId equals actor.Id
            where actor.Status == (int)ActorStatus.Active && userRole.EicFunctions.All(f => actor.MarketRoles.Any(marketRole => marketRole.Function == f.EicFunction))
            from permission in userRole.Permissions
            join permissionDetails in _marketParticipantDbContext.Permissions on (int)permission.Permission equals permissionDetails.Id
            where permissionDetails.EicFunctions.Any(f => userRole.EicFunctions.Any(eic => eic.EicFunction == f.EicFunction))
            select permission.Permission;

        return await query.ToListAsync().ConfigureAwait(false);
    }

    public Task<bool> IsFasAsync(Guid actorId, ExternalUserId externalUserId)
    {
        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            join a in _marketParticipantDbContext.Actors on r.ActorId equals a.Id
            where u.ExternalId == externalUserId.Value &&
                  a.Id == actorId &&
                  a.Status == (int)ActorStatus.Active
            select a.IsFas;

        return query.FirstOrDefaultAsync();
    }
}
