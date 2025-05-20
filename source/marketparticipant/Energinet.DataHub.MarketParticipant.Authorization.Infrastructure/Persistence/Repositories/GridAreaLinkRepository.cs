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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Repositories.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class GridAreaLinkRepository : IGridAreaLinkRepository
{
    private readonly IAuthorizationDbContext _marketParticipantDbContext;

    public GridAreaLinkRepository(IAuthorizationDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<GridAreaLink?> GetAsync(GridAreaLinkId id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        var gridAreaLink = await _marketParticipantDbContext.GridAreaLinks
            .FindAsync(id.Value)
            .ConfigureAwait(false);

        return gridAreaLink is null ? null : GridAreaLinkMapper.MapFromEntity(gridAreaLink);
    }

    public async Task<GridAreaLink?> GetAsync(GridAreaId id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        var query =
            from link in _marketParticipantDbContext.GridAreaLinks
            where link.GridAreaId == id.Value
            select link;

        var gridAreaLink = await query
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        return gridAreaLink is null ? null : GridAreaLinkMapper.MapFromEntity(gridAreaLink);
    }
}
