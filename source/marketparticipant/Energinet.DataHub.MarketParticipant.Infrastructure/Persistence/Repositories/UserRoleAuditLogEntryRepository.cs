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

            // Set permissions for created user role
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
            var userRolePermissionHistoryEntities = await _context
                .UserRolePermissionHistoryEntries
                .Where(up => up.UserRoleId == userRoleId.Value)
                .Select(d => new
                {
                    d.UserRoleId,
                    d.ChangedByIdentityId,
                    d.DeletedByIdentityId,
                    PeriodStart = DateTime.SpecifyKind(EF.Property<DateTime>(d, "PeriodStart"), DateTimeKind.Utc),
                    PeriodEnd = DateTime.SpecifyKind(EF.Property<DateTime>(d, "PeriodEnd"), DateTimeKind.Utc),
                    d.Permission,
                })
                .ToListAsync()
                .ConfigureAwait(false);

            var userRolePermissionEntities = await _context
                .UserRolePermissionEntries
                .Where(up => up.UserRoleId == userRoleId.Value)
                .Select(d => new
                {
                    d.UserRoleId,
                    d.ChangedByIdentityId,
                    d.DeletedByIdentityId,
                    PeriodStart = DateTime.SpecifyKind(EF.Property<DateTime>(d, "PeriodStart"), DateTimeKind.Utc),
                    PeriodEnd = DateTime.SpecifyKind(EF.Property<DateTime>(d, "PeriodEnd"), DateTimeKind.Utc),
                    d.Permission,
                })
                .ToListAsync()
                .ConfigureAwait(false);

            var userRolePermissionHistoryList = userRolePermissionHistoryEntities
                .Concat(userRolePermissionEntities)
                .ToList();

            var createdStateElements = userRolePermissionHistoryList
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

            var addedChanges = userRolePermissionHistoryList
                .Where(e => e.DeletedByIdentityId == null && e.PeriodStart != userRoleCreatedLogEntry.Timestamp)
                .Select(d => new
                {
                    d.ChangedByIdentityId,
                    d.DeletedByIdentityId,
                    Timestamp = d.PeriodStart,
                    d.Permission,
                }).ToList();
            var deletedChanges = userRolePermissionHistoryList
                .Where(e => e.DeletedByIdentityId != null)
                .Select(d => new
                {
                    d.ChangedByIdentityId,
                    d.DeletedByIdentityId,
                    Timestamp = d.PeriodStart,
                    d.Permission,
                }).ToList();

            var changes = addedChanges.Concat(deletedChanges).ToList();
            var groupChangesByTimestamp = changes
                .GroupBy(e => e.Timestamp)
                .OrderBy(e => e.Key);

            var prevState = createdPermissionState?.Permissions.ToList() ?? new List<PermissionId>();

            var result = new List<UserRoleAuditLogEntry>();
            foreach (var permissionChange in groupChangesByTimestamp)
            {
                var prevStateCopy = new List<PermissionId>(prevState);

                var deletedPermissionsIds = permissionChange.Where(e => e.DeletedByIdentityId != null).Select(e => e.Permission).ToList();
                var addedPermissionsIds = permissionChange.Where(e => e.DeletedByIdentityId == null).Select(e => e.Permission).ToList();

                prevStateCopy.RemoveAll(e => deletedPermissionsIds.Contains(e));
                prevStateCopy.AddRange(addedPermissionsIds);

                var userRoleAuditLogEntry = new UserRoleAuditLogEntry(
                        new UserRoleId(userRoleId.Value),
                        permissionChange.First().ChangedByIdentityId,
                        string.Empty,
                        string.Empty,
                        prevStateCopy,
                        null,
                        UserRoleStatus.Active,
                        UserRoleChangeType.PermissionsChange,
                        permissionChange.Key);

                prevState = prevStateCopy;
                result.Add(userRoleAuditLogEntry);
            }

            var permissionChanges = result
                .OrderBy(d => d.Timestamp)
                .ToList();

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
