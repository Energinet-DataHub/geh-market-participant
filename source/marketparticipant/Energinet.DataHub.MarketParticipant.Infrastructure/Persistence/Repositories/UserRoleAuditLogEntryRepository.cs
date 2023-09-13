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
            var userRoleAssignmentLogs = await _context.UserRoles
                .TemporalAll()
                .Where(e => e.Id == userRoleId.Value)
                .OrderBy(o => EF.Property<DateTime>(o, "PeriodStart"))
                .Select(log =>
                    new UserRoleAuditLogEntry(
                        new UserRoleId(log.Id),
                        log.ChangedByIdentityId,
                        log.Name,
                        log.Description,
                        log.Status,
                        UserRoleChangeType.Created,
                        EF.Property<DateTime>(log, "PeriodStart")))
                .ToListAsync().ConfigureAwait(false);

            var userRoleCurrentState = _context.UserRoles
                .Where(u => u.Id == userRoleId.Value)
                .Select(log =>
                    new UserRoleAuditLogEntry(
                        new UserRoleId(log.Id),
                        log.ChangedByIdentityId,
                        log.Name,
                        log.Description,
                        log.Status,
                        UserRoleChangeType.Created,
                        EF.Property<DateTime>(log, "PeriodStart")));

            userRoleAssignmentLogs.Add(userRoleCurrentState.Single());

            return BuildDiffLogEntries(userRoleAssignmentLogs);
        }

        private static IEnumerable<UserRoleAuditLogEntry> BuildDiffLogEntries(IReadOnlyList<UserRoleAuditLogEntry> logEntitiesOrdered)
        {
            for (var index = 0; index < logEntitiesOrdered.Count; index++)
            {
                var currentHistoryLog = logEntitiesOrdered[index];
                if (index == 0)
                {
                    yield return CreateAuditLogEntry(currentHistoryLog, UserRoleChangeType.Created);
                    continue;
                }

                var previousHistoryLog = logEntitiesOrdered[index - 1];

                if (currentHistoryLog.Name != previousHistoryLog.Name)
                {
                    yield return CreateAuditLogEntry(currentHistoryLog, UserRoleChangeType.NameChange);
                }

                if (currentHistoryLog.Description != previousHistoryLog.Description)
                {
                    yield return CreateAuditLogEntry(currentHistoryLog, UserRoleChangeType.DescriptionChange);
                }

                if (currentHistoryLog.Status != previousHistoryLog.Status)
                {
                    yield return CreateAuditLogEntry(currentHistoryLog, UserRoleChangeType.StatusChange);
                }
            }
        }

        private static UserRoleAuditLogEntry CreateAuditLogEntry(
            UserRoleAuditLogEntry userRoleEntity,
            UserRoleChangeType changeType)
        {
            return userRoleEntity with { ChangeType = changeType };
        }
    }
}
