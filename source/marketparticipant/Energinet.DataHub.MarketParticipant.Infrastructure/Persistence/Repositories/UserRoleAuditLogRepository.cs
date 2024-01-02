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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRoleAuditLogRepository : IUserRoleAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public UserRoleAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<UserRoleAuditedChange>>> GetAsync(UserRoleId userRoleId)
    {
        ArgumentNullException.ThrowIfNull(userRoleId);

        var userRoleAuditLogs = await GetUserRoleChangesAsync(userRoleId)
            .ConfigureAwait(false);

        var permissionAuditLogs = await GetPermissionChangesAsync(userRoleId)
            .ConfigureAwait(false);

        return userRoleAuditLogs.Concat(permissionAuditLogs);
    }

    private Task<IEnumerable<AuditLog<UserRoleAuditedChange>>> GetUserRoleChangesAsync(UserRoleId userRoleId)
    {
        var dataSource = new HistoryTableDataSource<UserRoleEntity>(_context.UserRoles, entity => entity.Id == userRoleId.Value);

        return new AuditLogBuilder<UserRoleAuditedChange, UserRoleEntity>(dataSource)
            .Add(UserRoleAuditedChange.Name, entity => entity.Name, AuditedChangeCompareAt.Creation)
            .Add(UserRoleAuditedChange.Description, entity => entity.Description, AuditedChangeCompareAt.Creation)
            .Add(UserRoleAuditedChange.Status, entity => entity.Status, AuditedChangeCompareAt.Creation)
            .BuildAsync();
    }

    private Task<IEnumerable<AuditLog<UserRoleAuditedChange>>> GetPermissionChangesAsync(UserRoleId userRoleId)
    {
        var dataSource = new HistoryTableDataSource<UserRolePermissionEntity>(_context.UserRolePermissionEntries, entity => entity.UserRoleId == userRoleId.Value);

        return new AuditLogBuilder<UserRoleAuditedChange, UserRolePermissionEntity>(dataSource)
            .Add(UserRoleAuditedChange.PermissionAdded, entity => (int)entity.Permission, AuditedChangeCompareAt.Creation)
            .Add(UserRoleAuditedChange.PermissionRemoved, entity => (int)entity.Permission, AuditedChangeCompareAt.Deletion)
            .WithGrouping(entity => entity.Permission)
            .BuildAsync();
    }
}
