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
using Energinet.DataHub.MarketParticipant.Domain.Model.Query;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;

public sealed class ActorQueryRepository : IActorQueryRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public ActorQueryRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<Domain.Model.Query.Actor?> GetActorAsync(Guid actorId)
    {
        var foundActor = await _marketParticipantDbContext
            .Actors
            .FirstOrDefaultAsync(actor => actor.Id == actorId)
            .ConfigureAwait(false);

        if (foundActor == null)
            return null;

        return new Domain.Model.Query.Actor(
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
