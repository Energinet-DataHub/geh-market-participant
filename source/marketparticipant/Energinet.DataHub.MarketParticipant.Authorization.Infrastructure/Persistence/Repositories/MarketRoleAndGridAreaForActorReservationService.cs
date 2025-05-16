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

using System;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class MarketRoleAndGridAreaForActorReservationService : IMarketRoleAndGridAreaForActorReservationService
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public MarketRoleAndGridAreaForActorReservationService(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<bool> TryReserveAsync(ActorId actorId, EicFunction marketRole, GridAreaId gridAreaId)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(gridAreaId);

        try
        {
            _marketParticipantDbContext.UniqueActorMarketRoleGridAreas.Add(new UniqueActorMarketRoleGridAreaEntity
            {
                ActorId = actorId.Value,
                MarketRoleFunction = (int)marketRole,
                GridAreaId = gridAreaId.Value
            });

            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException sqlException && sqlException.Errors.Cast<SqlError>().Any(x => x.Number == 2627))
        {
            return false;
        }
    }

    public async Task RemoveAllReservationsAsync(ActorId actorId)
    {
        var query =
            from u in _marketParticipantDbContext.UniqueActorMarketRoleGridAreas
            where u.ActorId == actorId.Value
            select u;

        var entities = await query.ToListAsync().ConfigureAwait(false);

        foreach (var entity in entities)
        {
            _marketParticipantDbContext.UniqueActorMarketRoleGridAreas.Remove(entity);
        }

        if (entities.Count > 0)
            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
