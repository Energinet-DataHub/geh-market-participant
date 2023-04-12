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

/// <summary>
/// Repository for inserting and querying user roles audit logs
/// </summary>
public interface IUserRoleAuditLogEntryRepository
{
    /// <summary>
    /// Inserts a <see cref="UserRoleAuditLogEntry"/>
    /// </summary>
    /// <param name="logEntries">The audit log entries.</param>
    Task InsertAuditLogEntriesAsync(IEnumerable<UserRoleAuditLogEntry> logEntries);

    /// <summary>
    /// Retrieves all log entries for a given user.
    /// </summary>
    /// <param name="userRoleId">The user role id to get the logs for.</param>
    Task<IEnumerable<UserRoleAuditLogEntry>> GetAsync(UserRoleId userRoleId);
}
