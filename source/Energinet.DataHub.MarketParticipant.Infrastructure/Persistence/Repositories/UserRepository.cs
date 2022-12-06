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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<UserId> AddOrUpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        UserEntity destination;

        if (user.Id.Value == default)
        {
            destination = new UserEntity();
        }
        else
        {
            destination = await BuildUserQuery()
                .FirstAsync(x => x.Id == user.Id.Value)
                .ConfigureAwait(false);
        }

        UserMapper.MapToEntity(user, destination);
        _marketParticipantDbContext.Users.Update(destination);
        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        return new UserId(destination.Id);
    }

    public async Task<User?> GetAsync(ExternalUserId externalUserId)
    {
        var userEntity = await BuildUserQuery()
            .SingleOrDefaultAsync(x => x.ExternalId == externalUserId.Value)
            .ConfigureAwait(false);

        return userEntity == null ? null : UserMapper.MapFromEntity(userEntity);
    }

    public async Task<User?> GetAsync(UserId userId)
    {
        var userEntity = await BuildUserQuery()
            .SingleOrDefaultAsync(x => x.Id == userId.Value)
            .ConfigureAwait(false);

        return userEntity == null ? null : UserMapper.MapFromEntity(userEntity);
    }

    private IQueryable<UserEntity> BuildUserQuery()
    {
        return _marketParticipantDbContext
            .Users
            .Include(u => u.RoleAssignments)
            .ThenInclude(r => r.UserRoleTemplate)
            .ThenInclude(t => t.Permissions);
    }
}
