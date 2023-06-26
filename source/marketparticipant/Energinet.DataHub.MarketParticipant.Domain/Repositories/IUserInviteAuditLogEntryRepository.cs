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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

public interface IUserInviteAuditLogEntryRepository
{
    /// <summary>
    /// Inserts a <see cref="UserInviteAuditLogEntry"/>
    /// </summary>
    /// <param name="logEntry">The audit log entry.</param>
    Task InsertAuditLogEntryAsync(UserInviteAuditLogEntry logEntry);

    /// <summary>
    /// Retrieves all log entries for a given user.
    /// </summary>
    /// <param name="userId">The user id to get the logs for.</param>
    Task<IEnumerable<UserInviteDetailsAuditLogEntry>> GetAsync(UserId userId);
}
