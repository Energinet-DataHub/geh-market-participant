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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;

public sealed class GetPermissionAuditLogsHandler
    : IRequestHandler<GetPermissionAuditLogsCommand, GetPermissionAuditLogsResponse>
{
    private readonly IPermissionAuditLogEntryRepository _permissionAuditLogsRepository;

    public GetPermissionAuditLogsHandler(IPermissionAuditLogEntryRepository permissionAuditLogsRepository)
    {
        _permissionAuditLogsRepository = permissionAuditLogsRepository;
    }

    public async Task<GetPermissionAuditLogsResponse> Handle(
        GetPermissionAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var permissionAuditLogs = await _permissionAuditLogsRepository
            .GetAsync((Permission)request.PermissionId)
            .ConfigureAwait(false);

        return new GetPermissionAuditLogsResponse(permissionAuditLogs.Select(auditLogEntry =>
            new PermissionAuditLogDto(
                (int)auditLogEntry.Permission,
                (int)auditLogEntry.PermissionChangeType,
                auditLogEntry.ChangedByUserId.Value,
                auditLogEntry.Timestamp)));
    }
}
