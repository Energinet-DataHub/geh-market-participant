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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

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

        public Task<IEnumerable<UserRoleAssignmentAuditLogEntry>> GetAsync(UserId userId)
        {
            throw new NotImplementedException();
        }

        private async Task InsertAuditLogAsync(UserRoleAssignmentAuditLogEntry logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            var entity = new UserRoleAssignmentAuditLogEntryEntity()
            {
                UserId = logEntry.UserId.Value,
                ActorId = logEntry.ActorId,
                UserRoleTemplateId = logEntry.UserRoleTemplateId.Value,
                Timestamp = logEntry.ChangedTimeOffset,
                ChangedByUserId = logEntry.ChangedByUserId.Value,
                AssignmentType = (int)logEntry.AssignmentType
            };

            _context.UserRoleAssignmentAuditLogEntries.Add(entity);

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
