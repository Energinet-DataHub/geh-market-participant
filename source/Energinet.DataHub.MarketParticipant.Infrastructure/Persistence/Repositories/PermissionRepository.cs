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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public PermissionRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public Task<IEnumerable<Permission>> GetAllAsync()
    {
        return GetAsync(KnownPermissions.All);
    }

    public Task<IEnumerable<Permission>> GetForMarketRoleAsync(EicFunction eicFunction)
    {
        var knownPermissions = KnownPermissions.All
            .Where(p => p.AssignableTo.Contains(eicFunction))
            .ToList();

        return GetAsync(knownPermissions);
    }

    public async Task<IEnumerable<Permission>> GetAsync(IEnumerable<PermissionId> permissions)
    {
        var knownPermissions = KnownPermissions.All
            .Where(p => permissions.Contains(p.Id))
            .ToList();

        var foundPermissions = await GetAsync(knownPermissions).ConfigureAwait(false);
        return foundPermissions;
    }

    public async Task<Permission> GetAsync(PermissionId permission)
    {
        var knownPermissions = KnownPermissions.All
            .Where(p => p.Id == permission)
            .ToList();

        var foundPermissions = await GetAsync(knownPermissions).ConfigureAwait(false);
        return foundPermissions.Single();
    }

    public async Task UpdatePermissionAsync(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        var entity = await _marketParticipantDbContext
            .Permissions
            .FindAsync(permission.Id)
            .ConfigureAwait(false);

        if (entity == null)
        {
            await _marketParticipantDbContext.Permissions
                .AddAsync(new PermissionEntity { Id = permission.Id, Description = permission.Description })
                .ConfigureAwait(false);
        }
        else
        {
            entity.Description = permission.Description;
        }

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    private async Task<IEnumerable<Permission>> GetAsync(IReadOnlyCollection<KnownPermission> wantedPermissions)
    {
        var dbPermissions = await BuildPermissionQuery(wantedPermissions)
            .ToDictionaryAsync(p => p.Id)
            .ConfigureAwait(false);

        var permissions = new List<Permission>();

        foreach (var permission in wantedPermissions)
        {
            if (dbPermissions.TryGetValue(permission.Id, out var dbPermission))
            {
                permissions.Add(new Permission(
                    permission.Id,
                    permission.Claim,
                    dbPermission.Description,
                    permission.Created,
                    permission.AssignableTo));
            }
            else
            {
                permissions.Add(new Permission(
                    permission.Id,
                    permission.Claim,
                    string.Empty,
                    permission.Created,
                    permission.AssignableTo));
            }
        }

        return permissions;
    }

    private IQueryable<PermissionEntity> BuildPermissionQuery(IEnumerable<KnownPermission> permissions)
    {
        var permissionIds = permissions
            .Select(p => p.Id)
            .ToList();

        return _marketParticipantDbContext
            .Permissions
            .Where(p => permissionIds.Contains(p.Id));
    }
}
