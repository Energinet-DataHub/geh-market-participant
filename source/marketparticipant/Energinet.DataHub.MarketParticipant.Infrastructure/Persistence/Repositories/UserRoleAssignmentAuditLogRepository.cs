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

public sealed class UserRoleAssignmentAuditLogRepository : IUserRoleAssignmentAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public UserRoleAssignmentAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<UserAuditedChange>>> GetAsync(UserId userId)
    {
        var dataSource = new HistoryTableDataSource<UserRoleAssignmentEntity>(_context.UserRoleAssignments, entity => entity.UserId == userId.Value);

        var auditLogs = await new AuditLogBuilder<UserAuditedChange, UserRoleAssignmentEntity>(dataSource)
            .Add(UserAuditedChange.UserRoleAssigned, AuditedChangeCompareAt.Creation, MakeAuditValue)
            .Add(UserAuditedChange.UserRoleRemoved, AuditedChangeCompareAt.Deletion, MakeAuditValue)
            .WithGrouping(_ => Guid.NewGuid()) // Each changed should be looked at separately.
            .BuildAsync()
            .ConfigureAwait(false);

        var extendedLogs = await GetExtendedAsync(userId).ConfigureAwait(false);
        return auditLogs
            .Concat(extendedLogs)
            .OrderBy(auditLog => auditLog.Timestamp);

        static string MakeAuditValue(UserRoleAssignmentEntity entity) => $"({entity.ActorId};{entity.UserRoleId})";
    }

    public Task AuditDeactivationAsync(UserId userId, AuditIdentity auditIdentity, UserRoleAssignment userRoleAssignment)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(auditIdentity);
        ArgumentNullException.ThrowIfNull(userRoleAssignment);

        var entity = new UserRoleAssignmentAuditLogEntryEntity
        {
            UserId = userId.Value,
            ActorId = userRoleAssignment.ActorId.Value,
            UserRoleId = userRoleAssignment.UserRoleId.Value,
            Timestamp = DateTimeOffset.UtcNow,
            ChangedByUserId = auditIdentity.Value,
            AssignmentType = (int)UserRoleAssignmentTypeAuditLog.RemovedDueToDeactivation
        };

        _context.UserRoleAssignmentAuditLogEntries.Add(entity);
        return _context.SaveChangesAsync();
    }

    private async Task<IEnumerable<AuditLog<UserAuditedChange>>> GetExtendedAsync(UserId userId)
    {
        var extendedAudit = await _context
            .UserRoleAssignmentAuditLogEntries
            .Where(log => log.UserId == userId.Value)
            .OrderBy(log => log.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        return extendedAudit.Select(extendedEntry => new AuditLog<UserAuditedChange>(
            UserAuditedChange.UserRoleRemovedDueToDeactivation,
            extendedEntry.Timestamp.ToInstant(),
            new AuditIdentity(extendedEntry.ChangedByUserId),
            false,
            null,
            extendedEntry.UserRoleId.ToString()));
    }
}
