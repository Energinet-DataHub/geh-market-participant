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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
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

        public Task InsertAuditLogEntryAsync(UserRoleAssignmentAuditLogEntry logEntry)
        {
            return InsertAuditLogAsync(logEntry);
        }

        public async Task<IEnumerable<UserRoleAssignmentAuditLogEntry>> GetAsync(UserId userId, Guid actorId)
        {
            var userRoleAssignmentLogs = _context.UserRoleAssignmentAuditLogEntries
                .Where(e => e.UserId == userId.Value && e.ActorId == actorId)
                .Select(log => new UserRoleAssignmentAuditLogEntry(
                    new UserId(log.UserId),
                    log.ActorId,
                    new UserRoleTemplateId(log.UserRoleTemplateId),
                    new UserId(log.ChangedByUserId),
                    log.Timestamp,
                    (UserRoleAssignmentTypeAuditLog)log.AssignmentType));

            return await userRoleAssignmentLogs.ToListAsync().ConfigureAwait(false);
        }

        private async Task InsertAuditLogAsync(UserRoleAssignmentAuditLogEntry logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            var entity = new UserRoleAssignmentAuditLogEntryEntity()
            {
                UserId = logEntry.UserId.Value,
                ActorId = logEntry.ActorId,
                UserRoleTemplateId = logEntry.UserRoleTemplateId.Value,
                Timestamp = logEntry.Timestamp,
                ChangedByUserId = logEntry.ChangedByUserId.Value,
                AssignmentType = (int)logEntry.AssignmentType
            };

            _context.UserRoleAssignmentAuditLogEntries.Add(entity);

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
