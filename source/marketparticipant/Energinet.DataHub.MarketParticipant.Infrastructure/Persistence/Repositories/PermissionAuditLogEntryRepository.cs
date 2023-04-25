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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class PermissionAuditLogEntryRepository : IPermissionAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public PermissionAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PermissionAuditLogEntry>> GetAsync(PermissionId permission)
        {
            var permissions = _context.PermissionAuditLogEntries
                .Where(p => p.PermissionId == permission);

            return await permissions
                .Select(p =>
                    new PermissionAuditLogEntry(
                        p.PermissionId,
                        new UserId(p.ChangedByUserId),
                        p.PermissionChangeType,
                        p.Timestamp,
                        p.Value)).ToListAsync().ConfigureAwait(false);
        }

        public Task InsertAuditLogEntryAsync(PermissionAuditLogEntry logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            var entity = new PermissionAuditLogEntryEntity
            {
                PermissionId = logEntry.Permission,
                PermissionChangeType = logEntry.PermissionChangeType,
                Timestamp = logEntry.Timestamp,
                ChangedByUserId = logEntry.ChangedByUserId.Value,
                Value = logEntry.Value
            };

            _context.PermissionAuditLogEntries.Add(entity);
            return _context.SaveChangesAsync();
        }
    }
}
