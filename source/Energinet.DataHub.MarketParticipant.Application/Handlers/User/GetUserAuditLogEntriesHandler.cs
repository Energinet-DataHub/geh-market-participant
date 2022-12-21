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

    public GetUserAuditLogEntriesHandler(IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository)
    {
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
    }

    public async Task<GetUserAuditLogResponse> Handle(
        GetUserAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _userRoleAssignmentAuditLogEntryRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        return new GetUserAuditLogResponse(auditLogs);
    }
}
