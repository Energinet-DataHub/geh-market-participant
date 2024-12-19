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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Options;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorConsolidationRepository : IActorConsolidationRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public ActorConsolidationRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<ActorConsolidationId> AddOrUpdateAsync(ActorConsolidation actorConsolidation)
    {
        ArgumentNullException.ThrowIfNull(actorConsolidation, nameof(actorConsolidation));

        ActorConsolidationEntity destination;
        if (actorConsolidation.Id.Value == default)
        {
            destination = new ActorConsolidationEntity();
        }
        else
        {
            destination = await _marketParticipantDbContext
                .ActorConsolidations
                .FindAsync(actorConsolidation.Id.Value)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"ActorConsolidation with id {actorConsolidation.Id.Value} is missing, even though it cannot be deleted.");
        }

        MapToEntity(actorConsolidation, destination);
        _marketParticipantDbContext.ActorConsolidations.Update(destination);

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);

        return new ActorConsolidationId(destination.Id);
    }

    public async Task<IEnumerable<ActorConsolidation>> GetAsync()
    {
        var query =
            from consolidation in _marketParticipantDbContext.ActorConsolidations
            select consolidation;

        var consolidations = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return consolidations.Select(MapFromEntity);
    }

    public async Task<ActorConsolidation?> GetAsync(ActorConsolidationId id)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        var actorConsolidation = await _marketParticipantDbContext.ActorConsolidations
            .FindAsync(id.Value)
            .ConfigureAwait(false);

        return actorConsolidation is null ? null : MapFromEntity(actorConsolidation);
    }

    public async Task<IEnumerable<ActorConsolidation>> GetReadyToConsolidateAsync()
    {
        var query =
            from consolidation in _marketParticipantDbContext.ActorConsolidations
            where consolidation.Status == ActorConsolidationStatus.Pending &&
                  consolidation.ConsolidateAt <= DateTimeOffset.UtcNow.AddMinutes(60 - int.Parse(ScheduleActorConsolidationOptions.ExecuteConsolidationTriggerMinutes, CultureInfo.InvariantCulture))
            select consolidation;

        var consolidations = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return consolidations.Select(MapFromEntity);
    }

    private static void MapToEntity(ActorConsolidation from, ActorConsolidationEntity destination)
    {
        destination.ActorFromId = from.ActorFromId.Value;
        destination.ActorToId = from.ActorToId.Value;
        destination.Status = from.Status;
        destination.ConsolidateAt = from.ConsolidateAt.ToDateTimeOffset();
    }

    private static ActorConsolidation MapFromEntity(ActorConsolidationEntity input)
    {
        return new ActorConsolidation(
            new ActorConsolidationId(input.Id),
            new ActorId(input.ActorFromId),
            new ActorId(input.ActorToId),
            input.ConsolidateAt.ToInstant(),
            input.Status);
    }
}
