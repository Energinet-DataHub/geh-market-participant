﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

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

        permissionAuditLogs = permissionAuditLogs.Where(log => ShouldDisplayPermission(log.CurrentValue) && ShouldDisplayPermission(log.PreviousValue));

        return userRoleAuditLogs.Concat(permissionAuditLogs);

        static bool ShouldDisplayPermission(string? value)
        {
            if (value == null)
                return true;

            // Skip permissions that are deprecated.
            var permission = (PermissionId)int.Parse(value, CultureInfo.InvariantCulture);
            return KnownPermissions.All.Any(kp => kp.Id == permission);
        }
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
            .WithGrouping(_ => Guid.NewGuid()) // Each changed should be looked at separately.
            .BuildAsync();
    }
}
