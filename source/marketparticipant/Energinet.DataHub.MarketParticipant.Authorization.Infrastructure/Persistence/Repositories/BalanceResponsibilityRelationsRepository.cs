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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class BalanceResponsibilityRelationsRepository : IBalanceResponsibilityRelationsRepository
{
    private readonly IBalanceResponsibilityRequestRepository _balanceResponsibilityRequestRepository;
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public BalanceResponsibilityRelationsRepository(
        IBalanceResponsibilityRequestRepository balanceResponsibilityRequestRepository,
        IMarketParticipantDbContext marketParticipantDbContext)
    {
        _balanceResponsibilityRequestRepository = balanceResponsibilityRequestRepository;
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<BalanceResponsibilityContractor> GetForBalanceResponsiblePartyAsync(ActorId balanceResponsibleParty)
    {
        await _balanceResponsibilityRequestRepository
            .ProcessNextRequestsAsync(balanceResponsibleParty)
            .ConfigureAwait(false);

        var relations = await _marketParticipantDbContext
            .BalanceResponsibilityRelations
            .Where(relation => relation.BalanceResponsiblePartyId == balanceResponsibleParty.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return new BalanceResponsibilityContractor(balanceResponsibleParty, relations.Select(Map));
    }

    public async Task<IEnumerable<BalanceResponsibilityContractor>> GetForEnergySupplierAsync(ActorId energySupplier)
    {
        await _balanceResponsibilityRequestRepository
            .ProcessNextRequestsAsync(energySupplier)
            .ConfigureAwait(false);

        var contractorsQuery = _marketParticipantDbContext
            .BalanceResponsibilityRelations
            .Where(relation => relation.EnergySupplierId == energySupplier.Value)
            .Select(relation => relation.BalanceResponsiblePartyId)
            .Distinct();

        var relationGroups = await _marketParticipantDbContext
            .BalanceResponsibilityRelations
            .Where(relation => contractorsQuery.Contains(relation.BalanceResponsiblePartyId))
            .GroupBy(relation => relation.BalanceResponsiblePartyId)
            .ToListAsync()
            .ConfigureAwait(false);

        return relationGroups.Select(relationGroup =>
            new BalanceResponsibilityContractor(
                new ActorId(relationGroup.Key),
                relationGroup.Select(Map)));
    }

    private static BalanceResponsibilityRelation Map(BalanceResponsibilityRelationEntity relation)
    {
        return new BalanceResponsibilityRelation(
            new ActorId(relation.EnergySupplierId),
            new GridAreaId(relation.GridAreaId),
            (MeteringPointType)relation.MeteringPointType,
            relation.ValidFrom.ToInstant(),
            relation.ValidTo?.ToInstant(),
            relation.ValidToAssignedAt?.ToInstant());
    }
}
