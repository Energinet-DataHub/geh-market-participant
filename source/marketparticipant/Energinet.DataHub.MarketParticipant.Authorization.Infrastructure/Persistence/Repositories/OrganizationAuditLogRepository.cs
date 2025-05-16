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

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class OrganizationAuditLogRepository : IOrganizationAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public OrganizationAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<OrganizationAuditedChange>>> GetAsync(OrganizationId organization)
    {
        var organizationAuditLogs = await GetOrganizationAuditsAsync(organization)
            .ConfigureAwait(false);

        var organizationDomainAuditLogs = await GetOrganizationDomainAuditsAsync(organization)
            .ConfigureAwait(false);

        return organizationAuditLogs.Concat(organizationDomainAuditLogs);
    }

    private Task<IEnumerable<AuditLog<OrganizationAuditedChange>>> GetOrganizationAuditsAsync(OrganizationId organization)
    {
        var organizationDataSource = new HistoryTableDataSource<OrganizationEntity>(_context.Organizations, entity => entity.Id == organization.Value);

        var organizationNameChanges = new AuditLogBuilder<OrganizationAuditedChange, OrganizationEntity>(organizationDataSource)
            .Add(OrganizationAuditedChange.Name, entity => entity.Name, AuditedChangeCompareAt.Creation)
            .BuildAsync();

        return organizationNameChanges;
    }

    private Task<IEnumerable<AuditLog<OrganizationAuditedChange>>> GetOrganizationDomainAuditsAsync(OrganizationId organization)
    {
        var organizationDomainDataSource = new HistoryTableDataSource<OrganizationDomainEntity>(_context.OrganizationDomains, entity => entity.OrganizationId == organization.Value);

        var organizationDomainChanges = new AuditLogBuilder<OrganizationAuditedChange, OrganizationDomainEntity>(organizationDomainDataSource)
            .Add(OrganizationAuditedChange.Domain, entity => entity.Domain, AuditedChangeCompareAt.BothCreationAndDeletion)
            .WithGrouping(entity => entity.Id)
            .BuildAsync();

        return organizationDomainChanges;
    }
}
