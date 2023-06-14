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

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserIdentityAuditLogEntryRepository : IUserIdentityAuditLogEntryRepository
{
    private readonly IMarketParticipantDbContext _context;

    public UserIdentityAuditLogEntryRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserIdentityAuditLogEntry>> GetAsync(UserId userId)
    {
        var logQuery =
            from log in _context.UserIdentityAuditLogEntries
            where log.UserId == userId.Value
            select new UserIdentityAuditLogEntry(
                new UserId(log.UserId),
                new UserId(log.ChangedByUserId),
                (UserIdentityAuditLogField)log.Field,
                log.NewValue,
                log.OldValue,
                log.Timestamp);

        return await logQuery.ToListAsync().ConfigureAwait(false);
    }

    public Task InsertAuditLogEntryAsync(UserIdentityAuditLogEntry logEntry)
    {
        ArgumentNullException.ThrowIfNull(logEntry);

        var entity = new UserIdentityAuditLogEntryEntity
        {
            UserId = logEntry.UserId.Value,
            Timestamp = logEntry.Timestamp,
            ChangedByUserId = logEntry.ChangedByUserId.Value,
            Field = (int)logEntry.Field,
            NewValue = logEntry.NewValue,
            OldValue = logEntry.OldValue
        };

        _context.UserIdentityAuditLogEntries.Add(entity);
        return _context.SaveChangesAsync();
    }
}
