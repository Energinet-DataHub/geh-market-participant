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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetUserAuditLogsHandler
    : IRequestHandler<GetUserAuditLogsCommand, GetUserAuditLogsResponse>
{
    private readonly IUserRoleAssignmentAuditLogRepository _userRoleAssignmentAuditLogRepository;
    private readonly IUserInviteAuditLogRepository _userInviteAuditLogRepository;
    private readonly IUserIdentityAuditLogRepository _userIdentityAuditLogRepository;

    public GetUserAuditLogsHandler(
        IUserRoleAssignmentAuditLogRepository userRoleAssignmentAuditLogRepository,
        IUserInviteAuditLogRepository userInviteAuditLogRepository,
        IUserIdentityAuditLogRepository userIdentityAuditLogRepository)
    {
        _userRoleAssignmentAuditLogRepository = userRoleAssignmentAuditLogRepository;
        _userInviteAuditLogRepository = userInviteAuditLogRepository;
        _userIdentityAuditLogRepository = userIdentityAuditLogRepository;
    }

    public async Task<GetUserAuditLogsResponse> Handle(
        GetUserAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = new UserId(request.UserId);

        var roleAssignmentAuditLogs = await _userRoleAssignmentAuditLogRepository
            .GetAsync(userId)
            .ConfigureAwait(false);

        var userInviteAuditLogs = await _userInviteAuditLogRepository
            .GetAsync(userId)
            .ConfigureAwait(false);

        var userIdentityAuditLogs = await _userIdentityAuditLogRepository
            .GetAsync(userId)
            .ConfigureAwait(false);

        var auditLogs = roleAssignmentAuditLogs
            .Concat(userInviteAuditLogs)
            .Concat(userIdentityAuditLogs)
            .OrderBy(x => x.Timestamp);

        return new GetUserAuditLogsResponse(auditLogs.Select(log => new AuditLogDto<UserAuditedChange>(log)));
    }
}
