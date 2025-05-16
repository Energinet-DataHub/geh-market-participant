﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class GridAreaAuditLogRepository : IGridAreaAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public GridAreaAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<AuditLog<GridAreaAuditedChange>>> GetAsync(GridAreaId gridAreaId)
    {
        var dataSource = new HistoryTableDataSource<GridAreaEntity>(_context.GridAreas, entity => entity.Id == gridAreaId.Value);

        return new AuditLogBuilder<GridAreaAuditedChange, GridAreaEntity>(dataSource)
            .Add(GridAreaAuditedChange.Name, entity => entity.Name)
            .Add(GridAreaAuditedChange.Decommissioned, entity => entity.ValidTo)
            .BuildAsync();
    }
}
