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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using User = Energinet.DataHub.MarketParticipant.Domain.Model.User;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<Guid> AddOrUpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        UserEntity? destination;
        destination = await GetQuery()
            .FirstOrDefaultAsync(x => x.Id == user.Id)
            .ConfigureAwait(false);

        if (destination is null)
        {
            destination = new UserEntity(user.Name);
            UserMapper.MapToEntity(user, destination);
            _marketParticipantDbContext.Users.Add(destination);
        }
        else
        {
            UserMapper.MapToEntity(user, destination);
            _marketParticipantDbContext.Users.Update(destination);
        }

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        return destination.Id;
    }

    public async Task<User?> GetAsync(Guid id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var result = await GetQuery()
            .FirstOrDefaultAsync(x => x.Id == id)
            .ConfigureAwait(false);

        if (result is null)
            return null;

        var roles = result.Actors.SelectMany(x => x.UserRoles.Select(y => y.UserRoleTemplateId)).Distinct();
        var roleTemplates = (await _marketParticipantDbContext.UserRoleTemplates
            .Where(x => roles.Contains(x.Id))
            .ToListAsync().ConfigureAwait(false)).ToDictionary(x => x.Id);

        var permissions = (await _marketParticipantDbContext.UserRoleTemplates
                .Where(x => roles.Contains(x.Id))
                .Include(x => x.Permissions)
                .ToListAsync()
                .ConfigureAwait(false))
            .SelectMany(x => x.Permissions).ToLookup(y => y.UserRoleTemplateId);
        return UserMapper.MapFromEntity(result, permissions, roleTemplates);
    }

    public async Task<IEnumerable<User>> GetAsync()
    {
        var result = await GetQuery()
            .OrderBy(x => x.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var roles = result.SelectMany(x => x.Actors.SelectMany(y => y.UserRoles.Select(z => z.UserRoleTemplateId)).Distinct());
        var roleTemplates = (await _marketParticipantDbContext.UserRoleTemplates
            .Where(x => roles.Contains(x.Id))
            .ToListAsync().ConfigureAwait(false)).ToDictionary(x => x.Id);
        var permissions = _marketParticipantDbContext.UserRoleTemplates
            .Where(x => roles.Contains(x.Id))
            .Include(x => x.Permissions)
            .ToList()
            .SelectMany(x => x.Permissions).ToLookup(y => y.UserRoleTemplateId);

        return result.Select(from => UserMapper.MapFromEntity(from, permissions, roleTemplates));
    }

    private IQueryable<UserEntity> GetQuery()
    {
        return _marketParticipantDbContext
            .Users
            .Include(x => x.Actors)
            .ThenInclude(x => x.UserRoles)
            .AsSingleQuery();
    }
}
