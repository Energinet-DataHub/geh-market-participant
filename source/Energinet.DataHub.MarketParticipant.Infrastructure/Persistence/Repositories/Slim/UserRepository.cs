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

    public async Task<IEnumerable<ExternalActorId>> GetActorsAsync(ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(externalUserId);

        var roleAssignmentsQuery = _marketParticipantDbContext
            .Users
            .Where(u => u.ExternalId == externalUserId.Value)
            .Include(u => u.RoleAssignments)
            .SelectMany(u => u.RoleAssignments);

        var externalActorIdQuery =
            from assignment in roleAssignmentsQuery
            join actor in _marketParticipantDbContext.Actors
                on assignment.ActorId equals actor.Id
            select actor.ActorId;

        var ids = await externalActorIdQuery
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);

        return ids.Select(id => new ExternalActorId(id!.Value));
    }

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(ExternalActorId externalActorId, ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(externalActorId);
        ArgumentNullException.ThrowIfNull(externalUserId);

        var actorId = await _marketParticipantDbContext
            .Actors
            .Where(x => x.ActorId == externalActorId.Value)
            .Select(x => x.Id)
            .SingleOrDefaultAsync().ConfigureAwait(false);

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
}
