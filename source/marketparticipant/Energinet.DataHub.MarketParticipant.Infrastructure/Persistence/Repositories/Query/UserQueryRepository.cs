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
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;

public sealed class UserQueryRepository : IUserQueryRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IPermissionRepository _permissionRepository;

    public UserQueryRepository(
        IMarketParticipantDbContext marketParticipantDbContext,
        IPermissionRepository permissionRepository)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _permissionRepository = permissionRepository;
    }

    public async Task<IEnumerable<ActorId>> GetActorsAsync(ExternalUserId externalUserId)
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

        return roleAssignmentsQuery.Select(id => new ActorId(id));
    }

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(ActorId actorId, ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(externalUserId);

        var userRoleQuery =
            from user in _marketParticipantDbContext.Users
            where user.ExternalId == externalUserId.Value
            join userRoleAssignment in _marketParticipantDbContext.UserRoleAssignments on user.Id equals userRoleAssignment.UserId
            where userRoleAssignment.ActorId == actorId.Value
            join userRole in _marketParticipantDbContext.UserRoles on userRoleAssignment.UserRoleId equals userRole.Id
            where userRole.Status == UserRoleStatus.Active
            join actor in _marketParticipantDbContext.Actors on userRoleAssignment.ActorId equals actor.Id
            where actor.Status == ActorStatus.Active && userRole.EicFunctions.All(f => actor.MarketRoles.Any(marketRole => marketRole.Function == f.EicFunction))
            select userRole;

        var userRoles = await userRoleQuery
            .Include(ur => ur.EicFunctions)
            .Include(ur => ur.Permissions)
            .ToListAsync()
            .ConfigureAwait(false);

        var userRolePermissions = userRoles
            .SelectMany(userRole => userRole.Permissions)
            .Select(permission => permission.Permission);

        var potentialPermissions = (await _permissionRepository
            .GetAsync(userRolePermissions)
            .ConfigureAwait(false))
            .ToDictionary(p => p.Id);

        var finalPermissions = new List<Permission>();

        foreach (var userRole in userRoles)
        {
            foreach (var permission in userRole.Permissions)
            {
                if (!potentialPermissions.TryGetValue(permission.Permission, out var knownPermission))
                    continue;

                if (!knownPermission.AssignableTo.Any(e => userRole.EicFunctions.Any(m => m.EicFunction == e)))
                    continue;

                finalPermissions.Add(knownPermission);
            }
        }

        return finalPermissions;
    }

    public Task<bool> IsFasAsync(ActorId actorId, ExternalUserId externalUserId)
    {
        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            join a in _marketParticipantDbContext.Actors on r.ActorId equals a.Id
            where u.ExternalId == externalUserId.Value &&
                  a.Id == actorId.Value &&
                  a.Status == ActorStatus.Active
            select a.IsFas;

        return query.FirstOrDefaultAsync();
    }
}
