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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetUserAuditLogEntriesHandler
    : IRequestHandler<GetUserAuditLogsCommand, GetUserAuditLogResponse>
{
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;
    private readonly IUserInviteAuditLogEntryRepository _userInviteAuditLogEntryRepository;

    public GetUserAuditLogEntriesHandler(
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository,
        IUserInviteAuditLogEntryRepository userInviteAuditLogEntryRepository)
    {
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
        _userInviteAuditLogEntryRepository = userInviteAuditLogEntryRepository;
    }

    public async Task<GetUserAuditLogResponse> Handle(
        GetUserAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var roleAssignmentAuditLogs = await _userRoleAssignmentAuditLogEntryRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        var userInviteLogs = await _userInviteAuditLogEntryRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        return new GetUserAuditLogResponse(roleAssignmentAuditLogs.Select(Map), userInviteLogs.Select(MapInvites));
    }

    private static UserRoleAssignmentAuditLogEntryDto Map(UserRoleAssignmentAuditLogEntry auditLogEntry)
    {
        return new UserRoleAssignmentAuditLogEntryDto(
            auditLogEntry.UserId.Value,
            auditLogEntry.ActorId.Value,
            auditLogEntry.UserRoleId.Value,
            auditLogEntry.ChangedByUserId.Value,
            auditLogEntry.Timestamp,
            auditLogEntry.AssignmentType);
    }

    private static UserInviteAuditLogEntryDto MapInvites(UserInviteDetailsAuditLogEntry auditLogEntry)
    {
        return new UserInviteAuditLogEntryDto(
            auditLogEntry.UserId.Value,
            auditLogEntry.ChangedByUserId.Value,
            auditLogEntry.ActorId.Value,
            auditLogEntry.ActorName,
            auditLogEntry.Timestamp);
    }
}
