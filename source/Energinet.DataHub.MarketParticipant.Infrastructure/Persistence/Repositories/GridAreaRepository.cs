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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class GridAreaRepository : IGridAreaRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public GridAreaRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<GridAreaId> AddOrUpdateAsync(GridArea gridArea)
        {
            Guard.ThrowIfNull(gridArea, nameof(gridArea));
            var gridEntity =
                await _marketParticipantDbContext.GridAreas.FirstOrDefaultAsync(entity =>
                    entity.Id == gridArea.Id.Value).ConfigureAwait(false) ?? new GridAreaEntity();

            _marketParticipantDbContext.GridAreas.Update(GridAreaMapper.MapToEntity(gridArea, gridEntity));
            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
            return new GridAreaId(gridEntity.Id);
        }

        public async Task<GridArea?> GetAsync(GridAreaId id)
        {
            var gridArea = await _marketParticipantDbContext.GridAreas
                .SingleOrDefaultAsync(s => s.Id == id.Value).ConfigureAwait(false);
            return gridArea is null ? null : GridAreaMapper.MapFromEntity(gridArea);
        }
    }
}
