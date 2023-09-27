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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public UserRepository(
        IAuditIdentityProvider auditIdentityProvider,
        IMarketParticipantDbContext marketParticipantDbContext,
        IUserIdentityRepository userIdentityRepository)
    {
        _auditIdentityProvider = auditIdentityProvider;
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

        var currentDbContext = (DbContext)_marketParticipantDbContext;
        IDbContextTransaction? currentTransaction = null;

        if (currentDbContext.Database.CurrentTransaction == null)
        {
            currentTransaction = await currentDbContext
                .Database
                .BeginTransactionAsync()
                .ConfigureAwait(false);
        }

        try
        {
            await UpdateUserRoleAssignmentsAsync(user.RoleAssignments, entity).ConfigureAwait(false);

            _marketParticipantDbContext.Users.Update(entity);

            await _marketParticipantDbContext
                .SaveChangesAsync()
                .ConfigureAwait(false);

            if (currentTransaction != null)
                await currentTransaction.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (currentTransaction != null)
                await currentTransaction.DisposeAsync().ConfigureAwait(false);

            throw;
        }

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

    private async Task UpdateUserRoleAssignmentsAsync(IEnumerable<UserRoleAssignment> userRoleAssignments, UserEntity userEntity)
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

        foreach (var removedUserRoleAssignment in removedUserRoleAssignments)
        {
            await _marketParticipantDbContext
                .UserRoleAssignments
                .Where(ura =>
                    ura.UserId == removedUserRoleAssignment.UserId &&
                    ura.ActorId == removedUserRoleAssignment.ActorId &&
                    ura.UserRoleId == removedUserRoleAssignment.UserRoleId)
                .ExecuteUpdateAsync(props => props.SetProperty(entity => entity.DeletedByIdentityId, _auditIdentityProvider.IdentityId.Value))
                .ConfigureAwait(false);
        }
    }
}
