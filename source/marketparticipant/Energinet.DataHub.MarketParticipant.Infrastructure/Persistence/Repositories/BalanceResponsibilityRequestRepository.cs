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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class BalanceResponsibilityRequestRepository : IBalanceResponsibilityRequestRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public BalanceResponsibilityRequestRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task EnqueueAsync(BalanceResponsibilityRequest balanceResponsibilityRequest)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsibilityRequest);

        var entity = new BalanceResponsibilityRequestEntity
        {
            EnergySupplier = balanceResponsibilityRequest.EnergySupplier.Value,
            BalanceResponsibleParty = balanceResponsibilityRequest.BalanceResponsibleParty.Value,
            GridAreaCode = balanceResponsibilityRequest.GridAreaCode.Value,
            MeteringPointType = (int)balanceResponsibilityRequest.MeteringPointType,
            ValidFrom = balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset(),
            ValidTo = balanceResponsibilityRequest.ValidTo?.ToDateTimeOffset()
        };

        await _marketParticipantDbContext
            .BalanceResponsibilityRequests
            .AddAsync(entity)
            .ConfigureAwait(false);

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    public async Task ProcessNextRequestsAsync(ActorId affectedActorId)
    {
        ArgumentNullException.ThrowIfNull(affectedActorId);

        while (await ProcessNextRequestAsync(affectedActorId).ConfigureAwait(false))
        {
        }
    }

    private async Task<bool> ProcessNextRequestAsync(ActorId affectedActorId)
    {
        ArgumentNullException.ThrowIfNull(affectedActorId);

        var balanceResponsibleRequestQuery =
            from balanceResponsibleRequest in _marketParticipantDbContext.BalanceResponsibilityRequests
            join energySupplier in _marketParticipantDbContext.Actors on
                new { ActorNumber = balanceResponsibleRequest.EnergySupplier, Function = EicFunction.EnergySupplier }
                equals
                new { energySupplier.ActorNumber, energySupplier.MarketRoles.Single().Function }
            join balanceResponsibleParty in _marketParticipantDbContext.Actors on
                new { ActorNumber = balanceResponsibleRequest.BalanceResponsibleParty, Function = EicFunction.BalanceResponsibleParty }
                equals
                new { balanceResponsibleParty.ActorNumber, balanceResponsibleParty.MarketRoles.Single().Function }
            join gridArea in _marketParticipantDbContext.GridAreas on
                balanceResponsibleRequest.GridAreaCode
                equals
                gridArea.Code
            where energySupplier.Id == affectedActorId.Value || balanceResponsibleParty.Id == affectedActorId.Value
            orderby balanceResponsibleRequest.Id
            select new { balanceResponsibleRequest, energySupplier, balanceResponsibleParty, gridArea };

        var nextBalanceResponsibleRequest =
            await balanceResponsibleRequestQuery
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

        if (nextBalanceResponsibleRequest == null)
            return false;

        _marketParticipantDbContext
            .BalanceResponsibilityRequests
            .Remove(nextBalanceResponsibleRequest.balanceResponsibleRequest);

        var nextBalanceResponsibleAgreement = new BalanceResponsibilityAgreementEntity
        {
            EnergySupplierId = nextBalanceResponsibleRequest.energySupplier.Id,
            BalanceResponsiblePartyId = nextBalanceResponsibleRequest.balanceResponsibleParty.Id,
            GridAreaId = nextBalanceResponsibleRequest.gridArea.Id,
            MeteringPointType = nextBalanceResponsibleRequest.balanceResponsibleRequest.MeteringPointType,
            ValidFrom = nextBalanceResponsibleRequest.balanceResponsibleRequest.ValidFrom,
            ValidTo = nextBalanceResponsibleRequest.balanceResponsibleRequest.ValidTo
        };

        // Check if copy exists, but what about merge?
        await _marketParticipantDbContext
            .BalanceResponsibilityAgreements
            .AddAsync(nextBalanceResponsibleAgreement)
            .ConfigureAwait(false);

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}
