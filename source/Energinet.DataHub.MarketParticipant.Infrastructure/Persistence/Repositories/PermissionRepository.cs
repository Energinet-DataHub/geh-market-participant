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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
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

    public async Task<IEnumerable<PermissionDetails>> GetAllAsync()
    {
        var permissions = await BuildPermissionQuery(null).ToListAsync().ConfigureAwait(false);
        return permissions.Select(x => new PermissionDetails(
            (Permission)x.Id,
            x.Description,
            x.EicFunctions.Select(y => y.EicFunction)));
    }

    public async Task<IEnumerable<PermissionDetails>> GetToMarketRoleAsync(EicFunction eicFunction)
    {
        var permissions = await BuildPermissionQuery(eicFunction).ToListAsync().ConfigureAwait(false);
        return permissions.Select(x => new PermissionDetails(
            (Permission)x.Id,
            x.Description,
            x.EicFunctions.Select(y => y.EicFunction)));
    }

    public async Task UpdatePermissionAsync(Permission permissionToUpdate, string newDescription)
    {
        var permissionId = (int)permissionToUpdate;
        var permission = await _marketParticipantDbContext.Permissions.FirstAsync(p => p.Id == permissionId).ConfigureAwait(false);
        permission.Description = newDescription;
        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private IQueryable<PermissionEntity> BuildPermissionQuery(EicFunction? eicFunction)
    {
        var query =
            from p in _marketParticipantDbContext.Permissions
            where eicFunction == null || p.EicFunctions.Any(x => x.EicFunction == eicFunction)
            select p;

        return query;
    }
}
