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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
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
            ValidTo = balanceResponsibilityRequest.ValidTo?.ToDateTimeOffset(),
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

        while (await DequeueNextAsync(affectedActorId).ConfigureAwait(false) is { } next)
        {
            await InsertAndHandleOverlapAsync(next).ConfigureAwait(false);
            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private async Task<BalanceResponsibilityRelationEntity?> DequeueNextAsync(ActorId affectedActorId)
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
            select new { Request = balanceResponsibleRequest, SupplierId = energySupplier.Id, BalanceResponsibleId = balanceResponsibleParty.Id, GridAreaId = gridArea.Id, };

        var nextBalanceResponsibleRequest =
            await balanceResponsibleRequestQuery
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

        if (nextBalanceResponsibleRequest == null)
            return null;

        _marketParticipantDbContext
            .BalanceResponsibilityRequests
            .Remove(nextBalanceResponsibleRequest.Request);

        return new BalanceResponsibilityRelationEntity
        {
            EnergySupplierId = nextBalanceResponsibleRequest.SupplierId,
            BalanceResponsiblePartyId = nextBalanceResponsibleRequest.BalanceResponsibleId,
            GridAreaId = nextBalanceResponsibleRequest.GridAreaId,
            MeteringPointType = nextBalanceResponsibleRequest.Request.MeteringPointType,
            ValidFrom = nextBalanceResponsibleRequest.Request.ValidFrom,
            ValidTo = nextBalanceResponsibleRequest.Request.ValidTo,
        };
    }

    private async Task InsertAndHandleOverlapAsync(BalanceResponsibilityRelationEntity entity)
    {
        var overlapQuery =
            from relation in _marketParticipantDbContext.BalanceResponsibilityRelations
            where
                relation.EnergySupplierId == entity.EnergySupplierId &&
                relation.BalanceResponsiblePartyId == entity.BalanceResponsiblePartyId &&
                relation.GridAreaId == entity.GridAreaId &&
                relation.MeteringPointType == entity.MeteringPointType &&
                relation.ValidFrom < (entity.ValidTo ?? DateTimeOffset.MaxValue) &&
                entity.ValidFrom < (relation.ValidTo ?? DateTimeOffset.MaxValue)
            select relation;

        var overlappedRelations = await overlapQuery
            .ToListAsync()
            .ConfigureAwait(false);

        if (overlappedRelations.Count > 1)
        {
            throw new ValidationException("Cannot process balance responsibility relation, as the overlap is not supported.")
                .WithErrorCode("balance_responsibility.unsupported_overlap")
                .WithArgs(
                    ("balanceResponsibleParty", entity.BalanceResponsiblePartyId),
                    ("energySupplier", entity.EnergySupplierId),
                    ("from", entity.ValidFrom),
                    ("to", entity.ValidTo ?? DateTimeOffset.MaxValue));
        }

        if (overlappedRelations.Count == 1)
        {
            var overlap = overlappedRelations[0];

            var supportedOverlapIdentical = overlap.ValidFrom == entity.ValidFrom && overlap.ValidTo == entity.ValidTo;
            if (supportedOverlapIdentical)
                return;

            var supportedOverlapEndDateSet = overlap.ValidFrom == entity.ValidFrom && overlap.ValidTo == null;
            if (supportedOverlapEndDateSet)
            {
                overlap.ValidTo = entity.ValidTo;
                _marketParticipantDbContext.BalanceResponsibilityRelations.Update(overlap);
                return;
            }

            throw new ValidationException("Cannot process balance responsibility relation, as the overlap is not supported.")
                .WithErrorCode("balance_responsibility.unsupported_overlap")
                .WithArgs(
                    ("balanceResponsibleParty", entity.BalanceResponsiblePartyId),
                    ("energySupplier", entity.EnergySupplierId),
                    ("from", entity.ValidFrom),
                    ("to", entity.ValidTo ?? DateTimeOffset.MaxValue));
        }

        await _marketParticipantDbContext
            .BalanceResponsibilityRelations
            .AddAsync(entity)
            .ConfigureAwait(false);
    }
}
