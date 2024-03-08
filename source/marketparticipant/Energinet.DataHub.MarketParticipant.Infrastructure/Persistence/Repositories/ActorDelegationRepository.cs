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
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorDelegationRepository : IActorDelegationRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public ActorDelegationRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<ActorDelegation?> GetAsync(ActorDelegationId actorDelegationId)
    {
        ArgumentNullException.ThrowIfNull(actorDelegationId, nameof(actorDelegationId));

        var actorDelegation = await _marketParticipantDbContext.ActorDelegations
            .FindAsync(actorDelegationId.Value)
            .ConfigureAwait(false);

        return actorDelegation is null ? null : ActorDelegationMapper.MapFromEntity(actorDelegation);
    }

    public async Task<IEnumerable<ActorDelegation>> GetDelegatedByAsync(ActorId actorId)
    {
        ArgumentNullException.ThrowIfNull(actorId, nameof(actorId));

        var actorDelegations = await _marketParticipantDbContext.ActorDelegations
            .Where(x => x.DelegatedByActorId == actorId.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return actorDelegations.Select(ActorDelegationMapper.MapFromEntity);
    }

    public async Task<IEnumerable<ActorDelegation>> GetDelegatedToAsync(ActorId actorId)
    {
        ArgumentNullException.ThrowIfNull(actorId, nameof(actorId));

        var actorDelegations = await _marketParticipantDbContext.ActorDelegations
            .Where(x => x.DelegatedToActorId == actorId.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return actorDelegations.Select(ActorDelegationMapper.MapFromEntity);
    }

    public async Task<ActorDelegationId> AddOrUpdateAsync(ActorDelegation actorDelegation)
    {
        ArgumentNullException.ThrowIfNull(actorDelegation);

        ActorDelegationEntity destination;

        if (actorDelegation.Id.Value == default)
        {
            destination = new ActorDelegationEntity();
        }
        else
        {
            destination = await _marketParticipantDbContext
                .ActorDelegations
                .FindAsync(actorDelegation.Id.Value)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"ActorDelegation with id {actorDelegation.Id.Value} is missing, even though it cannot be deleted.");
        }

        ActorDelegationMapper.MapToEntity(actorDelegation, destination);
        _marketParticipantDbContext.ActorDelegations.Update(destination);

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);

        return new ActorDelegationId(destination.Id);
    }
}
