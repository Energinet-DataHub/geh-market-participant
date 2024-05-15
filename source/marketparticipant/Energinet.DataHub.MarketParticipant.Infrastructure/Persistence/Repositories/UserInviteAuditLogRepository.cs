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
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserInviteAuditLogRepository : IUserInviteAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public UserInviteAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<UserAuditedChange>>> GetAsync(UserId userId)
    {
        var entities = await _context
            .UserInviteAuditLogEntries
            .Where(log => log.UserId == userId.Value)
            .OrderBy(log => log.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var auditLogs = new List<AuditLog<UserAuditedChange>>();
        var isFirstInvite = false;

        foreach (var entity in entities)
        {
            var auditLog = new AuditLog<UserAuditedChange>(
                UserAuditedChange.InvitedIntoActor,
                entity.Timestamp.ToInstant(),
                new AuditIdentity(entity.ChangedByUserId),
                isFirstInvite,
                entity.ActorId.ToString(),
                null);

            isFirstInvite = false;
            auditLogs.Add(auditLog);
        }

        return auditLogs;
    }

    public Task AuditAsync(UserId userId, AuditIdentity auditIdentity, ActorId invitedInto)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(auditIdentity);
        ArgumentNullException.ThrowIfNull(invitedInto);

        var entity = new UserInviteAuditLogEntryEntity
        {
            ChangedByUserId = auditIdentity.Value,
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId.Value,
            ActorId = invitedInto.Value,
        };

        _context.UserInviteAuditLogEntries.Add(entity);
        return _context.SaveChangesAsync();
    }
}
