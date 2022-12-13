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
using Energinet.DataHub.MarketParticipant.Domain.Model.Slim;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;
using Microsoft.EntityFrameworkCore;
using Actor = Energinet.DataHub.MarketParticipant.Domain.Model.Slim.Actor;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Slim;

public sealed class ActorRepository : IActorRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public ActorRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<Actor?> GetActorAsync(Guid actorId)
    {
        var foundActor = await _marketParticipantDbContext
            .Actors
            .FirstOrDefaultAsync(actor => actor.Id == actorId)
            .ConfigureAwait(false);

        if (foundActor == null)
            return null;

        return new Actor(
            new OrganizationId(foundActor.OrganizationId),
            foundActor.Id,
            (ActorStatus)foundActor.Status);
    }

    public async Task<IEnumerable<SelectionActor>> GetSelectionActorsAsync(IEnumerable<Guid> actorIds)
    {
        var ids = actorIds.Distinct().ToList();

        var actors = await _marketParticipantDbContext
            .Actors
            .Where(x => ids.Contains(x.Id))
            .ToListAsync()
            .ConfigureAwait(false);

        return actors.Select(x => new SelectionActor(x.Id, x.ActorNumber, x.Name));
    }
}
