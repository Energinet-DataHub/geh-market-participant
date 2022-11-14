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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
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

    public async Task<Actor?> GetActorAsync(Guid externalActorId)
    {
        var foundActor = await _marketParticipantDbContext
            .Actors
            .FirstOrDefaultAsync(actor => actor.ActorId == externalActorId)
            .ConfigureAwait(false);

        if (foundActor == null)
            return null;

        return new Actor(foundActor.OrganizationId, foundActor.Id, (ActorStatus)foundActor.Status);
    }
}
