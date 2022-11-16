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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserActorRepository : IUserActorRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserActorRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task AddOrUpdateAsync(UserActor userActor)
    {
        ArgumentNullException.ThrowIfNull(userActor);

        UserActorEntity? destination;
        destination = await GetQuery()
            .FirstOrDefaultAsync(x => x.Id == userActor.Id)
            .ConfigureAwait(false);

        // if (destination is null)
        // {
        //     destination = new PermissionEntity(permission.Id, permission.Description);
        //     _marketParticipantDbContext.Permissions.Add(destination);
        // }
        // else
        // {
        //     destination.Description = permission.Description;
        //     _marketParticipantDbContext.Permissions.Update(destination);
        // }
        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<UserActor?> GetAsync(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var result = await _marketParticipantDbContext
            .Permissions
            .FindAsync(id)
            .ConfigureAwait(false);
        return result is null
            ? null
            : new UserActor(new List<UserRoleTemplate>());
    }

    public async Task<IEnumerable<UserActor>> GetAsync()
    {
        var result = await _marketParticipantDbContext
            .Permissions
            .OrderBy(x => x.Id)
            .ToListAsync()
            .ConfigureAwait(false);
        return result.Select(x => new UserActor(new List<UserRoleTemplate>()));
    }

    private IQueryable<UserActorEntity> GetQuery()
    {
        return _marketParticipantDbContext
            .UserActors
            .Include(x => x.UserRoleTemplates)
            .AsSingleQuery();
    }
}
