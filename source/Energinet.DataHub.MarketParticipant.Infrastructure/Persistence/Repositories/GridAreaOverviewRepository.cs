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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class GridAreaOverviewRepository : IGridAreaOverviewRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public GridAreaOverviewRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<IEnumerable<GridAreaOverviewItem>> GetAsync()
        {
            var q = from g in _marketParticipantDbContext.GridAreas
                    join l in _marketParticipantDbContext.MarketRoleGridAreas on g.Id equals l.GridAreaId into lgroup
                    from l in lgroup.DefaultIfEmpty()
                    join r in _marketParticipantDbContext.MarketRoles on l.MarketRoleId equals r.Id into rgroup
                    from r in rgroup.DefaultIfEmpty()
                    join a in _marketParticipantDbContext.Actors on r.ActorInfoId equals a.Id into agroup
                    from a in agroup.DefaultIfEmpty()
                    where a == null || r.Function == (int)EicFunction.GridAccessProvider
                    select new { g, a, r };

            var result = await q.ToListAsync().ConfigureAwait(false);

            return result.GroupBy(x => x.g.Id).Select(x =>
            {
                var ga = x.FirstOrDefault(x => x.r?.Function == (int)EicFunction.GridAccessProvider) ?? x.First();
                var grid = ga.g;
                var actor = ga.a;
                return new GridAreaOverviewItem(
                    new GridAreaId(grid.Id),
                    new GridAreaName(grid.Name),
                    new GridAreaCode(grid.Code),
                    grid.PriceAreaCode,
                    grid.ValidFrom,
                    grid.ValidTo,
                    actor != null ? new ActorNumber(ga.a.ActorNumber) : null,
                    actor != null ? new ActorName(ga.a.Name) : null);
            });
        }
    }
}
