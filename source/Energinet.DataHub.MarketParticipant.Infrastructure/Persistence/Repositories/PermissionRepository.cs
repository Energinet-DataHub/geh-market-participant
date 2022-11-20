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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
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

    public async Task AddOrUpdateAsync(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        PermissionEntity? destination;
        destination = await _marketParticipantDbContext
            .Permissions
            .FindAsync(permission.Id)
            .ConfigureAwait(false);

        if (destination is null)
        {
            destination = new PermissionEntity(permission.Id, permission.Description);
            _marketParticipantDbContext.Permissions.Add(destination);
        }
        else
        {
            destination.Description = permission.Description;
            _marketParticipantDbContext.Permissions.Update(destination);
        }

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<Permission?> GetAsync(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var result = await _marketParticipantDbContext
            .Permissions
            .FindAsync(id)
            .ConfigureAwait(false);
        return result is null
            ? null
            : new Permission(result.Id, result.Description);
    }

    public async Task<IEnumerable<Permission>> GetAsync()
    {
        var result = await _marketParticipantDbContext
            .Permissions
            .OrderBy(x => x.Id)
            .ToListAsync()
            .ConfigureAwait(false);
        return result.Select(x => new Permission(x.Id, x.Description));
    }
}
