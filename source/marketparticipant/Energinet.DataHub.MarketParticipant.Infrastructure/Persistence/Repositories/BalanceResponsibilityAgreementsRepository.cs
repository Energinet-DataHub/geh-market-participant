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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class BalanceResponsibilityAgreementsRepository : IBalanceResponsibilityAgreementsRepository
{
    private readonly IBalanceResponsibilityRequestRepository _balanceResponsibilityRequestRepository;
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public BalanceResponsibilityAgreementsRepository(
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

        var agreements = await _marketParticipantDbContext
            .BalanceResponsibilityAgreements
            .Where(agreement => agreement.BalanceResponsiblePartyId == balanceResponsibleParty.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return new BalanceResponsibilityContractor(balanceResponsibleParty, agreements.Select(Map));
    }

    public async Task<IEnumerable<BalanceResponsibilityContractor>> GetForEnergySupplierAsync(ActorId energySupplier)
    {
        await _balanceResponsibilityRequestRepository
            .ProcessNextRequestsAsync(energySupplier)
            .ConfigureAwait(false);

        var contractorsQuery = _marketParticipantDbContext
            .BalanceResponsibilityAgreements
            .Where(agreement => agreement.EnergySupplierId == energySupplier.Value)
            .Select(agreement => agreement.BalanceResponsiblePartyId)
            .Distinct();

        var agreementGroups = await _marketParticipantDbContext
            .BalanceResponsibilityAgreements
            .Where(agreement => contractorsQuery.Contains(agreement.BalanceResponsiblePartyId))
            .GroupBy(agreement => agreement.BalanceResponsiblePartyId)
            .ToListAsync()
            .ConfigureAwait(false);

        return agreementGroups.Select(agreementGroup =>
            new BalanceResponsibilityContractor(
                new ActorId(agreementGroup.Key),
                agreementGroup.Select(Map)));
    }

    private static BalanceResponsibilityAgreement Map(BalanceResponsibilityAgreementEntity agreement)
    {
        return new BalanceResponsibilityAgreement(
            new ActorId(agreement.EnergySupplierId),
            new GridAreaId(agreement.GridAreaId),
            (MeteringPointType)agreement.MeteringPointType,
            agreement.ValidFrom.ToInstant(),
            agreement.ValidTo?.ToInstant());
    }
}
