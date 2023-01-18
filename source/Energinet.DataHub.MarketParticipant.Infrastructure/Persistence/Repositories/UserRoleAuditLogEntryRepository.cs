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
    public sealed class UserRoleAuditLogEntryRepository : IUserRoleAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public UserRoleAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserRoleAuditLogEntry>> GetAsync(UserRoleId userRoleId)
        {
            var userRoleAssignmentLogs = _context.UserRoleAuditLogEntries
                .Where(e => e.UserRoleId == userRoleId.Value)
                .Select(log => new UserRoleAuditLogEntry(
                    new UserRoleId(log.UserRoleId),
                    new UserId(log.ChangedByUserId),
                    log.Timestamp,
                    (UserRoleChangeType)log.UserRoleChangeType,
                    log.ChangeDescriptionJson));

            return await userRoleAssignmentLogs.ToListAsync().ConfigureAwait(false);
        }

        public Task InsertAuditLogEntriesAsync(IEnumerable<UserRoleAuditLogEntry> logEntries)
        {
            ArgumentNullException.ThrowIfNull(logEntries);

            foreach (var logEntry in logEntries)
            {
                var entity = new UserRoleAuditLogEntryEntity
                {
                    UserRoleId = logEntry.UserRoleId.Value,
                    Timestamp = logEntry.Timestamp,
                    ChangedByUserId = logEntry.ChangedByUserId.Value,
                    UserRoleChangeType = (int)logEntry.UserRoleChangeType,
                    ChangeDescriptionJson = logEntry.ChangeDescriptionJson
                };

                _context.UserRoleAuditLogEntries.Add(entity);
            }

            return _context.SaveChangesAsync();
        }
    }
}
