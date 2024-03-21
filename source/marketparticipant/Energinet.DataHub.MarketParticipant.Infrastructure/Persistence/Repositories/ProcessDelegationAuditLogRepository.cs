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

public sealed class ProcessDelegationAuditLogRepository : IProcessDelegationAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;
    private readonly IProcessDelegationRepository _processDelegationRepository;

    public ProcessDelegationAuditLogRepository(
        IMarketParticipantDbContext context,
        IProcessDelegationRepository processDelegationRepository)
    {
        _context = context;
        _processDelegationRepository = processDelegationRepository;
    }

    public async Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetAsync(ActorId actor)
    {
        var processDelegations = await _processDelegationRepository
            .GetForActorAsync(actor)
            .ConfigureAwait(false);

        var allAudits = new List<AuditLog<ActorAuditedChange>>();

        foreach (var processDelegation in processDelegations)
        {
            foreach (var delegationPeriod in processDelegation.Delegations)
            {
                var dataSource = new HistoryTableDataSource<DelegationPeriodEntity>(
                    _context.DelegationPeriods,
                    entity => entity.Id == delegationPeriod.Id.Value);

                string? AuditedValueSelector(DelegationPeriodEntity entity) => $"({entity.StartsAt};{entity.GridAreaId};{processDelegation.Process}{(entity.StopsAt != null ? $";{entity.StopsAt}" : string.Empty)})";

                var audits = await new AuditLogBuilder<ActorAuditedChange, DelegationPeriodEntity>(dataSource)
                    .Add(ActorAuditedChange.DelegationStart, entity => entity.StartsAt, AuditedValueSelector, AuditedChangeCompareAt.Creation)
                    .Add(ActorAuditedChange.DelegationStop, entity => entity.StopsAt, AuditedValueSelector, AuditedChangeCompareAt.ChangeOnly)
                    .WithGrouping(entity => entity.Id)
                    .BuildAsync()
                    .ConfigureAwait(false);

                allAudits.AddRange(audits);
            }
        }

        return allAudits;
    }
}
