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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public UserRepository(
        IMarketParticipantDbContext marketParticipantDbContext,
        IUserIdentityRepository userIdentityRepository)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task<UserId> AddOrUpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entity = await GetOrNewEntityAsync(user).ConfigureAwait(false);

        entity.Id = user.Id.Value;
        entity.ExternalId = user.ExternalId.Value;
        entity.AdministratedByActorId = user.AdministratedBy.Value;
        entity.MitIdSignupInitiatedAt = user.MitIdSignupInitiatedAt;
        entity.InvitationExpiresAt = user.InvitationExpiresAt;

        UpdateUserRoleAssignments(user.RoleAssignments, entity);

        _marketParticipantDbContext.Users.Update(entity);

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);

        return new UserId(entity.Id);
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

    public async Task<IEnumerable<User>> GetToUserRoleAsync(UserRoleId userRoleId)
    {
        var userEntities = await BuildUserQuery()
            .Where(x => x.RoleAssignments.Any(y => y.UserRoleId == userRoleId.Value))
            .ToListAsync()
            .ConfigureAwait(false);

        return userEntities.Select(UserMapper.MapFromEntity);
    }

    public async Task<IEnumerable<User>> FindUsersWithExpiredInvitationAsync()
    {
        var userEntities = await BuildUserQuery()
            .Where(u => u.InvitationExpiresAt < DateTimeOffset.UtcNow)
            .ToListAsync()
            .ConfigureAwait(false);

        return userEntities.Select(UserMapper.MapFromEntity);
    }

    private static void UpdateUserRoleAssignments(IEnumerable<UserRoleAssignment> userRoleAssignments, UserEntity userEntity)
    {
        var removedUserRoleAssignments = new HashSet<UserRoleAssignmentEntity>(userEntity.RoleAssignments);

        userEntity.RoleAssignments.Clear();

        foreach (var userRoleAssignment in userRoleAssignments)
        {
            var userRoleAssignmentToKeep = removedUserRoleAssignments.FirstOrDefault(
                existing =>
                    existing.ActorId == userRoleAssignment.ActorId.Value &&
                    existing.UserRoleId == userRoleAssignment.UserRoleId.Value);

            if (userRoleAssignmentToKeep != null)
            {
                removedUserRoleAssignments.Remove(userRoleAssignmentToKeep);
                userEntity.RoleAssignments.Add(userRoleAssignmentToKeep);
            }
            else
            {
                userEntity.RoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    UserId = userEntity.Id,
                    ActorId = userRoleAssignment.ActorId.Value,
                    UserRoleId = userRoleAssignment.UserRoleId.Value
                });
            }
        }
    }

    private IQueryable<UserEntity> BuildUserQuery()
    {
        return _marketParticipantDbContext
            .Users
            .Include(u => u.RoleAssignments);
    }

    private async Task<UserEntity> GetOrNewEntityAsync(User user)
    {
        if (user.Id.Value == default)
        {
            var identity = await _userIdentityRepository
                .GetAsync(user.ExternalId)
                .ConfigureAwait(false);

            return new UserEntity
            {
                Email = identity!.Email.Address,
                SharedReferenceId = user.SharedId.Value
            };
        }

        return await BuildUserQuery()
            .FirstAsync(x => x.Id == user.Id.Value)
            .ConfigureAwait(false);
    }
}
