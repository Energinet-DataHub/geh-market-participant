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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class PermissionAuditLogRepository : IPermissionAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public PermissionAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<AuditLog<PermissionAuditedChange>>> GetAsync(PermissionId permission)
    {
        var knownPermission = KnownPermissions.All.Single(kp => kp.Id == permission);
        var initialPermission = new PermissionEntity
        {
            Id = permission,
            ChangedByIdentityId = KnownAuditIdentityProvider.Migration.IdentityId.Value
        };

        var dataSource = new HistoryTableDataSource<PermissionEntity>(_context.Permissions, entity => entity.Id == permission);

        return new AuditLogBuilder<PermissionAuditedChange, PermissionEntity>(dataSource)
            .WithInitial(initialPermission, knownPermission.Created)
            .Add(PermissionAuditedChange.Claim, _ => knownPermission.Claim, AuditedChangeCompareAt.Creation)
            .Add(PermissionAuditedChange.Description, entity => entity.Description)
            .BuildAsync();
    }
}
