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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorDelegationAuditLogRepository : IActorDelegationAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;
    private readonly IMessageDelegationRepository _messageDelegationRepository;

    public ActorDelegationAuditLogRepository(
        IMarketParticipantDbContext context,
        IMessageDelegationRepository messageDelegationRepository)
    {
        _context = context;
        _messageDelegationRepository = messageDelegationRepository;
    }

    public async Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetAsync(ActorId actor)
    {
        var messageDelegations = await _messageDelegationRepository
            .GetForActorAsync(actor)
            .ConfigureAwait(false);

        var allAudits = new List<AuditLog<ActorAuditedChange>>();

        foreach (var messageDelegation in messageDelegations)
        {
            foreach (var delegationPeriod in messageDelegation.Delegations)
            {
                var dataSource = new HistoryTableDataSource<DelegationPeriodEntity>(
                    _context.DelegationPeriods,
                    entity => entity.Id == delegationPeriod.Id.Value);

                var audits = await new AuditLogBuilder<ActorAuditedChange, DelegationPeriodEntity>(dataSource)
                    .Add(ActorAuditedChange.DelegationActorTo, entity => entity.DelegatedToActorId, AuditedChangeCompareAt.Creation)
                    .Add(ActorAuditedChange.DelegationMessageType, _ => messageDelegation.MessageType, AuditedChangeCompareAt.Creation)
                    .Add(ActorAuditedChange.DelegationStart, entity => entity.StartsAt, AuditedChangeCompareAt.Creation)
                    .Add(ActorAuditedChange.DelegationStop, entity => entity.StopsAt)
                    .WithGrouping(entity => entity.Id)
                    .BuildAsync()
                    .ConfigureAwait(false);

                allAudits.AddRange(audits);
            }
        }

        return allAudits;
    }
}
