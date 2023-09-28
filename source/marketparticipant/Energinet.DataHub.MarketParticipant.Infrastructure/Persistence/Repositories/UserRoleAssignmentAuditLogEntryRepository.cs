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
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class UserRoleAssignmentAuditLogEntryRepository : IUserRoleAssignmentAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public UserRoleAssignmentAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserRoleAssignmentAuditLogEntry>> GetAsync(UserId userId)
        {
            var allHistory = await _context.UserRoleAssignments
                .ReadAllHistoryForAsync(ura => ura.UserId == userId.Value)
                .ConfigureAwait(false);

            var userRoleAssignmentLogs = new List<UserRoleAssignmentAuditLogEntry>();

            foreach (var (entity, periodStart) in allHistory)
            {
                if (entity.DeletedByIdentityId == null)
                {
                    userRoleAssignmentLogs.Add(new UserRoleAssignmentAuditLogEntry(
                        new UserId(entity.UserId),
                        new ActorId(entity.ActorId),
                        new UserRoleId(entity.UserRoleId),
                        new AuditIdentity(entity.ChangedByIdentityId),
                        periodStart,
                        UserRoleAssignmentTypeAuditLog.Added));
                }
                else
                {
                    userRoleAssignmentLogs.Add(new UserRoleAssignmentAuditLogEntry(
                        new UserId(entity.UserId),
                        new ActorId(entity.ActorId),
                        new UserRoleId(entity.UserRoleId),
                        new AuditIdentity(entity.DeletedByIdentityId.Value),
                        periodStart,
                        UserRoleAssignmentTypeAuditLog.Removed));
                }
            }

            var extendedAudit = await _context
                .UserRoleAssignmentAuditLogEntries
                .Where(log => log.UserId == userId.Value)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var extendedEntry in extendedAudit)
            {
                userRoleAssignmentLogs.Add(new UserRoleAssignmentAuditLogEntry(
                    new UserId(extendedEntry.UserId),
                    new ActorId(extendedEntry.ActorId),
                    new UserRoleId(extendedEntry.UserRoleId),
                    new AuditIdentity(extendedEntry.ChangedByUserId),
                    extendedEntry.Timestamp,
                    (UserRoleAssignmentTypeAuditLog)extendedEntry.AssignmentType));
            }

            return userRoleAssignmentLogs.OrderBy(log => log.Timestamp);
        }

        public Task InsertAuditLogEntryAsync(UserId userId, UserRoleAssignmentAuditLogEntry logEntry)
        {
            ArgumentNullException.ThrowIfNull(userId);
            ArgumentNullException.ThrowIfNull(logEntry);

            var entity = new UserRoleAssignmentAuditLogEntryEntity
            {
                UserId = userId.Value,
                ActorId = logEntry.ActorId.Value,
                UserRoleId = logEntry.UserRoleId.Value,
                Timestamp = logEntry.Timestamp,
                ChangedByUserId = logEntry.AuditIdentity.Value,
                AssignmentType = (int)logEntry.AssignmentType
            };

            _context.UserRoleAssignmentAuditLogEntries.Add(entity);

            return _context.SaveChangesAsync();
        }
    }
}
