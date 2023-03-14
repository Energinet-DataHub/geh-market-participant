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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserRoleRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<IEnumerable<UserRole>> GetAllAsync()
    {
        var userRoles = await BuildUserRoleQuery().ToListAsync().ConfigureAwait(false);
        return userRoles.Select(MapUserRole);
    }

    public async Task<UserRole?> GetAsync(UserRoleId userRoleId)
    {
        var userRole = await BuildUserRoleQuery()
            .SingleOrDefaultAsync(t => t.Id == userRoleId.Value)
            .ConfigureAwait(false);

        return userRole == null
            ? null
            : MapUserRole(userRole);
    }

    public async Task<UserRole?> GetByNameAsync(string userRoleName)
    {
        var userRole = await BuildUserRoleQuery()
            .SingleOrDefaultAsync(r => r.Name == userRoleName)
            .ConfigureAwait(false);

        return userRole == null
            ? null
            : MapUserRole(userRole);
    }

    public async Task<IEnumerable<UserRole>> GetAsync(IEnumerable<EicFunction> eicFunctions)
    {
        var userRoles = await BuildUserRoleQuery()
            .Where(t => t
                .EicFunctions
                .Select(f => f.EicFunction)
                .All(f => eicFunctions.Contains(f)))
            .ToListAsync()
            .ConfigureAwait(false);

        return userRoles.Select(MapUserRole);
    }

    public async Task<UserRoleId> AddAsync(UserRole userRole)
    {
        ArgumentNullException.ThrowIfNull(userRole);
        var role = new UserRoleEntity()
        {
            Name = userRole.Name,
            Description = userRole.Description,
            Status = userRole.Status,
        };
        foreach (var permissionEntity in userRole.Permissions.Select(x => new UserRolePermissionEntity() { Permission = x }))
        {
            role.Permissions.Add(permissionEntity);
        }

        role.EicFunctions.Add(new UserRoleEicFunctionEntity() { EicFunction = userRole.EicFunction });
        _marketParticipantDbContext.UserRoles.Add(role);
        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        return new UserRoleId(role.Id);
    }

    public async Task UpdateAsync(UserRole userRoleUpdate)
    {
        ArgumentNullException.ThrowIfNull(userRoleUpdate);

        var userRoleEntity = await BuildUserRoleQuery()
            .SingleOrDefaultAsync(t => t.Id == userRoleUpdate.Id.Value)
            .ConfigureAwait(false);

        if (userRoleEntity != null)
        {
            userRoleEntity.Name = userRoleUpdate.Name;
            userRoleEntity.Description = userRoleUpdate.Description;
            userRoleEntity.Status = userRoleUpdate.Status;

            userRoleEntity.Permissions.Clear();
            var permissionsToAdd = userRoleUpdate.Permissions.Select(x => new UserRolePermissionEntity { Permission = x });
            foreach (var permissionEntity in permissionsToAdd)
            {
                userRoleEntity.Permissions.Add(permissionEntity);
            }

            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        else
        {
            throw new ArgumentException("User role not found");
        }
    }

    public async Task<IEnumerable<UserRole>> GetAsync(Permission permission)
    {
        var userRoles = await BuildUserRoleQuery()
            .Where(t => t
                .Permissions
                .Any(f => f.Permission == permission))
            .ToListAsync()
            .ConfigureAwait(false);

        return userRoles.Select(MapUserRole);
    }

    private static UserRole MapUserRole(UserRoleEntity userRole)
    {
        return new UserRole(
            new UserRoleId(userRole.Id),
            userRole.Name,
            userRole.Description ?? string.Empty,
            userRole.Status,
            userRole.Permissions.Select(p => p.Permission).ToList(),
            userRole.EicFunctions.First().EicFunction);
    }

    private IQueryable<UserRoleEntity> BuildUserRoleQuery()
    {
        return _marketParticipantDbContext
            .UserRoles
            .Include(x => x.EicFunctions)
            .Include(x => x.Permissions);
    }
}
