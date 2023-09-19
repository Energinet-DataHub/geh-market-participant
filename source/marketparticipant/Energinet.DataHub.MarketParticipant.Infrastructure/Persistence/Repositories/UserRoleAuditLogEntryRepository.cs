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
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
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
            var userRoleAuditLogs = await GetUserRoleChangesAndStateAsync(userRoleId).ConfigureAwait(false);
            var createdUserRoleLog = userRoleAuditLogs.First(e => e.ChangeType == UserRoleChangeType.Created);

            // GetPermission changes
            var permissionsChanges = await GetPermissionChangesAsync(userRoleId, createdUserRoleLog).ConfigureAwait(false);

            // Get EicFunction changes
            var eicFunctionChanges = await GetEicFunctionChangesAsync(userRoleId).ConfigureAwait(false);

            // Set permissions for created user role
            var auditLogs = MergeCreatedStateAndLists(userRoleAuditLogs, permissionsChanges, eicFunctionChanges);

            return auditLogs
                .OrderBy(a => a.Timestamp)
                .ThenBy(e => e.ChangeType);
        }

        private static IEnumerable<UserRoleAuditLogEntry> MergeCreatedStateAndLists(
            List<UserRoleAuditLogEntry> userRoleAuditLogs,
            ICollection<UserRoleAuditLogEntry> permissionsChanges,
            ICollection<UserRoleAuditLogEntry> eicFunctionChanges)
        {
            var createdUserRoleLog = userRoleAuditLogs.Find(u => u.ChangeType == UserRoleChangeType.Created)!;
            var createdWithPermissions = permissionsChanges.FirstOrDefault(l => l.ChangeType == UserRoleChangeType.Created);
            var createdWithEicFunction = eicFunctionChanges.MinBy(l => l.EicFunction);

            if (permissionsChanges.Any() && createdWithPermissions != null)
            {
                permissionsChanges.Remove(createdWithPermissions);
            }

            if (eicFunctionChanges.Any() && createdWithEicFunction != null)
            {
                eicFunctionChanges.Remove(createdWithEicFunction);
            }

            var createdUserRoleLogWithPermissions = createdUserRoleLog with
            {
                Permissions = createdWithPermissions?.Permissions ?? Enumerable.Empty<PermissionId>(),
                EicFunction = createdWithEicFunction?.EicFunction,
            };

            userRoleAuditLogs[0] = createdUserRoleLogWithPermissions;
            userRoleAuditLogs.AddRange(permissionsChanges);
            userRoleAuditLogs.AddRange(eicFunctionChanges);

            return userRoleAuditLogs;
        }

        private async Task<List<UserRoleAuditLogEntry>> GetUserRoleChangesAndStateAsync(UserRoleId userRoleId)
        {
            var emptyPermissions = Enumerable.Empty<PermissionId>();

            UserRoleAuditLogEntry CopyAndSetChangeType(UserRoleAuditLogEntry userRoleEntity, UserRoleChangeType changeType) => userRoleEntity with { ChangeType = changeType };

            var userRoleAssignmentLogsOrdered = await _context
                .UserRoles
                .TemporalAll()
                .Where(e => e.Id == userRoleId.Value)
                .OrderBy(o => EF.Property<DateTime>(o, "PeriodStart"))
                .Select(log =>
                    new UserRoleAuditLogEntry(
                        new UserRoleId(log.Id),
                        log.ChangedByIdentityId,
                        log.Name,
                        log.Description,
                        emptyPermissions,
                        null,
                        log.Status,
                        UserRoleChangeType.Created,
                        EF.Property<DateTime>(log, "PeriodStart")))
                .ToListAsync().ConfigureAwait(false);

            // TODO Timestamp in changes should be EndDate ?
            var logs = new List<UserRoleAuditLogEntry>();

            for (var index = 0; index < userRoleAssignmentLogsOrdered.Count; index++)
            {
                var currentHistoryLog = userRoleAssignmentLogsOrdered[index];
                if (index == 0)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, UserRoleChangeType.Created));
                    continue;
                }

                var previousHistoryLog = userRoleAssignmentLogsOrdered[index - 1];

                if (currentHistoryLog.Name != previousHistoryLog.Name)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, UserRoleChangeType.NameChange));
                }

                if (currentHistoryLog.Description != previousHistoryLog.Description)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, UserRoleChangeType.DescriptionChange));
                }

                if (currentHistoryLog.Status != previousHistoryLog.Status)
                {
                    logs.Add(CopyAndSetChangeType(currentHistoryLog, UserRoleChangeType.StatusChange));
                }
            }

            return logs;
        }

        private async Task<List<UserRoleAuditLogEntry>> GetPermissionChangesAsync(UserRoleId userRoleId, UserRoleAuditLogEntry userRoleCreatedLogEntry)
        {
            var userRolePermissionChangesEntities = await _context
                .UserRolePermissionEntries
                .TemporalAll()
                .Where(up => up.UserRoleId == userRoleId.Value)
                .Select(d => new
                {
                    d.UserRoleId,
                    d.ChangedByIdentityId,
                    PeriodStart = DateTime.SpecifyKind(EF.Property<DateTime>(d, "PeriodStart"), DateTimeKind.Utc),
                    PeriodEnd = DateTime.SpecifyKind(EF.Property<DateTime>(d, "PeriodEnd"), DateTimeKind.Utc),
                    d.Permission,
                })
                .ToListAsync()
                .ConfigureAwait(false);

            var createdStateElements = userRolePermissionChangesEntities
                .Where(e => userRoleCreatedLogEntry.Timestamp.Equals(DateTime.SpecifyKind(e.PeriodStart, DateTimeKind.Utc)))
                .ToList();
            var createdPermissionState = createdStateElements.Any() ? new UserRoleAuditLogEntry(
                new UserRoleId(userRoleId.Value),
                createdStateElements.First().ChangedByIdentityId,
                string.Empty,
                string.Empty,
                createdStateElements.Select(p => p.Permission),
                null,
                UserRoleStatus.Active,
                UserRoleChangeType.Created,
                createdStateElements.First().PeriodStart) : null;

            var createdWithPermissions = createdPermissionState?.Permissions.Select(e => e);

            var userRolePermissionChangesDic = userRolePermissionChangesEntities
                .GroupBy(up => up.PeriodEnd)
                .OrderBy(d => d.Key)
                //.Where(e => e.Key < DateTimeOffset.MaxValue.AddDays(-1000))
                .ToDictionary(g => g.Key, g =>
                    new UserRoleAuditLogEntry(
                        new UserRoleId(userRoleId.Value),
                        g.First().ChangedByIdentityId,
                        string.Empty,
                        string.Empty,
                        g.Select(p => p.Permission),
                        null,
                        UserRoleStatus.Active,
                        UserRoleChangeType.PermissionsChange,
                        g.Key));

            var prevPermissions = createdWithPermissions?.ToList() ?? new List<PermissionId>();

            var tempList = new List<UserRoleAuditLogEntry>();
            foreach (var permissionChange in userRolePermissionChangesDic)
            {
                var tt = prevPermissions.Except(permissionChange.Value.Permissions);
                tempList.Add(new UserRoleAuditLogEntry(
                    new UserRoleId(userRoleId.Value),
                    permissionChange.Value.ChangedByIdentityId,
                    string.Empty,
                    string.Empty,
                    tt,
                    null,
                    UserRoleStatus.Active,
                    UserRoleChangeType.PermissionsChange,
                    permissionChange.Key));
                prevPermissions = permissionChange.Value.Permissions.ToList();
            }

            /*var permissionChanges = userRolePermissionChangesDic.Values
                .OrderBy(d => d.Timestamp)
                .ToList();*/

            var permissionChanges = tempList
                .OrderBy(d => d.Timestamp)
                .ToList();

            if (createdPermissionState != null)
            {
                permissionChanges.Add(createdPermissionState);
            }

            return permissionChanges;
        }

        private async Task<List<UserRoleAuditLogEntry>> GetEicFunctionChangesAsync(UserRoleId userRoleId)
        {
            var emptyPermissions = Enumerable.Empty<PermissionId>();
            var userRoleEicFunctionChanges = await _context
                .UserRoleEicFunctionEntries
                .TemporalAll()
                .Where(up => up.UserRoleId == userRoleId.Value)
                .Select(log => new UserRoleAuditLogEntry(
                    new UserRoleId(userRoleId.Value),
                    log.ChangedByIdentityId,
                    string.Empty,
                    string.Empty,
                    emptyPermissions,
                    log.EicFunction,
                    UserRoleStatus.Active,
                    UserRoleChangeType.EicFunctionChange,
                    EF.Property<DateTime>(log, "PeriodStart")))
                .ToListAsync()
                .ConfigureAwait(false);

            return userRoleEicFunctionChanges;
        }
    }
}
