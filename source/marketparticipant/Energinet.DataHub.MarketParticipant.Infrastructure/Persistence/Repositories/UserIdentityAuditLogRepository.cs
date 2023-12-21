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

public sealed class UserIdentityAuditLogRepository : IUserIdentityAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public UserIdentityAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<UserAuditedChange>>> GetAsync(UserId userId)
    {
        var entities = await _context
            .UserIdentityAuditLogEntries
            .Where(log => log.UserId == userId.Value)
            .OrderBy(log => log.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var auditLogs = new List<AuditLog<UserAuditedChange>>();

        var isFirstNameSet = false;
        var isLastNameSet = false;
        var isPhoneNumberSet = false;

        foreach (var entity in entities)
        {
            var change = Map((UserIdentityAuditLogField)entity.Field);
            var isInitialAssignment =
                (!isFirstNameSet && change == UserAuditedChange.FirstName) ||
                (!isLastNameSet && change == UserAuditedChange.LastName) ||
                (!isPhoneNumberSet && change == UserAuditedChange.PhoneNumber);

            var auditLog = new AuditLog<UserAuditedChange>(
                change,
                entity.Timestamp.ToInstant(),
                new AuditIdentity(entity.ChangedByUserId),
                isInitialAssignment,
                entity.NewValue,
                entity.OldValue);

            switch (change)
            {
                case UserAuditedChange.FirstName:
                    isFirstNameSet = true;
                    break;
                case UserAuditedChange.LastName:
                    isLastNameSet = true;
                    break;
                case UserAuditedChange.PhoneNumber:
                    isPhoneNumberSet = true;
                    break;
            }

            auditLogs.Add(auditLog);
        }

        return auditLogs;

        static UserAuditedChange Map(UserIdentityAuditLogField field)
        {
            return field switch
            {
                UserIdentityAuditLogField.FirstName => UserAuditedChange.FirstName,
                UserIdentityAuditLogField.LastName => UserAuditedChange.LastName,
                UserIdentityAuditLogField.PhoneNumber => UserAuditedChange.PhoneNumber,
                UserIdentityAuditLogField.Status => UserAuditedChange.Status,
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };
        }
    }

    public Task AuditAsync(
        UserId userId,
        AuditIdentity auditIdentity,
        UserAuditedChange change,
        string? currentValue,
        string? previousValue)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(auditIdentity);

        var entity = new UserIdentityAuditLogEntryEntity
        {
            UserId = userId.Value,
            Timestamp = DateTimeOffset.UtcNow,
            ChangedByUserId = auditIdentity.Value,
            Field = (int)Map(change),
            NewValue = currentValue ?? string.Empty,
            OldValue = previousValue ?? string.Empty
        };

        _context.UserIdentityAuditLogEntries.Add(entity);
        return _context.SaveChangesAsync();

        static UserIdentityAuditLogField Map(UserAuditedChange change)
        {
            return change switch
            {
                UserAuditedChange.FirstName => UserIdentityAuditLogField.FirstName,
                UserAuditedChange.LastName => UserIdentityAuditLogField.LastName,
                UserAuditedChange.PhoneNumber => UserIdentityAuditLogField.PhoneNumber,
                UserAuditedChange.Status => UserIdentityAuditLogField.Status,
                _ => throw new ArgumentOutOfRangeException(nameof(change)),
            };
        }
    }
}
