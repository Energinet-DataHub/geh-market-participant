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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Repositories.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public class GridAreaRepository : IGridAreaRepository
{
    private readonly IAuthorizationDbContext _marketParticipantDbContext;

    public GridAreaRepository(IAuthorizationDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<GridArea?> GetAsync(GridAreaId id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        var gridArea = await _marketParticipantDbContext.GridAreas
            .FindAsync(id.Value)
            .ConfigureAwait(false);

        return gridArea is null ? null : GridAreaMapper.MapFromEntity(gridArea);
    }

    public async Task<IEnumerable<GridArea>> GetAsync()
    {
        var entities = await _marketParticipantDbContext
            .GridAreas
            .ToListAsync()
            .ConfigureAwait(false);

        return entities.Select(GridAreaMapper.MapFromEntity);
    }
}
