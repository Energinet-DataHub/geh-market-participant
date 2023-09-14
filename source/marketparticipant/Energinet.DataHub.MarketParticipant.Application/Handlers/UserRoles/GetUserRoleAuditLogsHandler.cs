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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class GetUserRoleAuditLogsHandler
    : IRequestHandler<GetUserRoleAuditLogsCommand, GetUserRoleAuditLogsResponse>
{
    private readonly IUserRoleAuditLogEntryRepository _userRoleAuditLogEntryRepository;

    public GetUserRoleAuditLogsHandler(IUserRoleAuditLogEntryRepository userRoleAuditLogEntryRepository)
    {
        _userRoleAuditLogEntryRepository = userRoleAuditLogEntryRepository;
    }

    public async Task<GetUserRoleAuditLogsResponse> Handle(
        GetUserRoleAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _userRoleAuditLogEntryRepository
            .GetAsync(new UserRoleId(request.UserRoleId))
            .ConfigureAwait(false);

        return new GetUserRoleAuditLogsResponse(auditLogs.Select(Map));
    }

    private static UserRoleAuditLogEntryDto Map(UserRoleAuditLogEntry auditLogEntry)
    {
        return new UserRoleAuditLogEntryDto(
            auditLogEntry.UserRoleId.Value,
            auditLogEntry.ChangedByIdentityId,
            auditLogEntry.Name,
            auditLogEntry.Description,
            auditLogEntry.Permissions,
            auditLogEntry.Status,
            auditLogEntry.ChangeType,
            auditLogEntry.Timestamp);
    }
}
