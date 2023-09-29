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
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class UserRoleAuditLogEntryRepository : IUserRoleAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public UserRoleAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserRoleAuditLogEntry>> GetAsync(UserRoleId userRoleId)
        {
            ArgumentNullException.ThrowIfNull(userRoleId);

            // Build audit logs for user roles
            var userRoleChangesAuditLogs = await GetUserRoleChangesAndStateAsync(userRoleId).ConfigureAwait(false);
            var createdUserRoleLog = userRoleChangesAuditLogs.First(e => e.ChangeType == UserRoleChangeType.Created);

            // GetPermission changes
            var permissionsChanges = await GetPermissionChangesAsync(userRoleId, createdUserRoleLog).ConfigureAwait(false);

            // Get EicFunction for userRole
            var readOnlyEicFunction = await GetEicFunctionForUserRoleAsync(userRoleId).ConfigureAwait(false);

            // Set permissions for created user role and merge logs
            var auditLogs = MergeCreatedStateAndLists(userRoleChangesAuditLogs, permissionsChanges, readOnlyEicFunction);

            return auditLogs
                .OrderBy(a => a.Timestamp)
                .ThenBy(e => e.ChangeType);
        }

        private static IEnumerable<UserRoleAuditLogEntry> MergeCreatedStateAndLists(
            List<UserRoleAuditLogEntry> userRoleAuditLogs,
            ICollection<UserRoleAuditLogEntry> permissionsChanges,
            EicFunction readOnlyEicFunction)
        {
            var createdUserRoleLog = userRoleAuditLogs.Find(u => u.ChangeType == UserRoleChangeType.Created)!;
            var createdWithPermissions = permissionsChanges.FirstOrDefault(l => l.ChangeType == UserRoleChangeType.Created);

            if (permissionsChanges.Any() && createdWithPermissions != null)
            {
                permissionsChanges.Remove(createdWithPermissions);
            }

            var createdUserRoleLogWithPermissions = createdUserRoleLog with
            {
                Permissions = createdWithPermissions?.Permissions ?? Enumerable.Empty<PermissionId>(),
                EicFunction = readOnlyEicFunction,
            };

            userRoleAuditLogs[0] = createdUserRoleLogWithPermissions;
            userRoleAuditLogs.AddRange(permissionsChanges);

            return userRoleAuditLogs;
        }

        private async Task<List<UserRoleAuditLogEntry>> GetUserRoleChangesAndStateAsync(UserRoleId userRoleId)
        {
            var historicEntities = await _context.UserRoles
                .ReadAllHistoryForAsync(entity => entity.Id == userRoleId.Value)
                .ConfigureAwait(false);

            UserRoleAuditLogEntry CopyAndSetChangeType(UserRoleEntity userRoleEntity, DateTimeOffset changeTimeStamp, UserRoleChangeType changeType) => new(
                new UserRoleId(userRoleEntity.Id),
                userRoleEntity.ChangedByIdentityId,
                userRoleEntity.Name,
                userRoleEntity.Description,
                Enumerable.Empty<PermissionId>(),
                null,
                userRoleEntity.Status,
                changeType,
                changeTimeStamp);

            var logs = new List<UserRoleAuditLogEntry>();

            for (var index = 0; index < historicEntities.Count; index++)
            {
                var currentHistoryLog = historicEntities[index].Entity;
                var currentPeriodStart = historicEntities[index].PeriodStart;

                if (index == 0)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, currentPeriodStart, UserRoleChangeType.Created));
                    continue;
                }

                var previousHistoryLog = historicEntities[index - 1].Entity;

                if (currentHistoryLog.Name != previousHistoryLog.Name)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, currentPeriodStart, UserRoleChangeType.NameChange));
                }

                if (currentHistoryLog.Description != previousHistoryLog.Description)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, currentPeriodStart, UserRoleChangeType.DescriptionChange));
                }

                if (currentHistoryLog.Status != previousHistoryLog.Status)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, currentPeriodStart, UserRoleChangeType.StatusChange));
                }
            }

            return logs;
        }

        private async Task<List<UserRoleAuditLogEntry>> GetPermissionChangesAsync(UserRoleId userRoleId, UserRoleAuditLogEntry userRoleCreatedLogEntry)
        {
            var userRolePermissionHistoryList = await _context.UserRolePermissionEntries
                .ReadAllHistoryForAsync(entity => entity.UserRoleId == userRoleId.Value)
                .ConfigureAwait(false);

            var createdStateElements = userRolePermissionHistoryList
                .Where(e => userRoleCreatedLogEntry.Timestamp.Equals(e.PeriodStart))
                .ToList();
            var createdPermissionState = createdStateElements.Any() ? new UserRoleAuditLogEntry(
                new UserRoleId(userRoleId.Value),
                createdStateElements.First().Entity.ChangedByIdentityId,
                string.Empty,
                string.Empty,
                createdStateElements.Select(p => p.Entity.Permission),
                null,
                UserRoleStatus.Active,
                UserRoleChangeType.Created,
                userRoleCreatedLogEntry.Timestamp) : null;

            var addedChanges = userRolePermissionHistoryList
                .Where(e => e.Entity.DeletedByIdentityId == null && e.PeriodStart != userRoleCreatedLogEntry.Timestamp)
                .Select(d => new UserRoleAuditLogEntry(
                    new UserRoleId(userRoleId.Value),
                    d.Entity.ChangedByIdentityId,
                    string.Empty,
                    string.Empty,
                    new List<PermissionId>() { d.Entity.Permission },
                    null,
                    UserRoleStatus.Active,
                    UserRoleChangeType.PermissionAdded,
                    d.PeriodStart));
            var deletedChanges = userRolePermissionHistoryList
                .Where(e => e.Entity.DeletedByIdentityId != null)
                .Select(d => new UserRoleAuditLogEntry(
                    new UserRoleId(userRoleId.Value),
                    d.Entity.DeletedByIdentityId ?? d.Entity.ChangedByIdentityId,
                    string.Empty,
                    string.Empty,
                    new List<PermissionId>() { d.Entity.Permission },
                    null,
                    UserRoleStatus.Active,
                    UserRoleChangeType.PermissionRemoved,
                    d.PeriodStart));

            var permissionChanges = addedChanges.Concat(deletedChanges).ToList();

            if (createdPermissionState != null)
            {
                permissionChanges.Add(createdPermissionState);
            }

            return permissionChanges;
        }

        private async Task<EicFunction> GetEicFunctionForUserRoleAsync(UserRoleId userRoleId)
        {
            var userRole = await _context
                .UserRoles
                .Include(u => u.EicFunctions)
                .FirstAsync(up => up.Id == userRoleId.Value)
                .ConfigureAwait(false);

            return userRole.EicFunctions.First().EicFunction;
        }
    }
}
