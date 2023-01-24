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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class UpdateUserRoleHandler
    : IRequestHandler<UpdateUserRoleCommand, UpdateUserRoleResponse>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRoleAuditLogService _userRoleAuditLogService;
    private readonly IUserRoleAuditLogEntryRepository _userRoleAuditLogEntryRepository;

    public UpdateUserRoleHandler(
        IUserRoleRepository userRoleRepository,
        IUserRoleAuditLogService userRoleAuditLogService,
        IUserRoleAuditLogEntryRepository userRoleAuditLogEntryRepository)
    {
        _userRoleRepository = userRoleRepository;
        _userRoleAuditLogService = userRoleAuditLogService;
        _userRoleAuditLogEntryRepository = userRoleAuditLogEntryRepository;
    }

    public async Task<UpdateUserRoleResponse> Handle(
        UpdateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRole = await _userRoleRepository.GetAsync(new UserRoleId(request.UserRoleId)).ConfigureAwait(false);

        if (userRole == null)
        {
            throw new NotFoundValidationException(request.UserRoleId);
        }

        var updatedUserRole = new UserRole(
            new UserRoleId(request.UserRoleId),
            request.UserRoleDto.Name,
            request.UserRoleDto.Description,
            request.UserRoleDto.Status,
            request.UserRoleDto.Permissions.Select(Enum.Parse<Permission>),
            userRole.EicFunction);

        var createdUserRoleId = await _userRoleRepository
            .UpdateAsync(updatedUserRole)
            .ConfigureAwait(false);

        var auditLogs = _userRoleAuditLogService.BuildAuditLogsForUserRoleChanged(new UserId(request.EditingUserId), userRole, updatedUserRole);
        await _userRoleAuditLogEntryRepository.InsertAuditLogEntriesAsync(auditLogs).ConfigureAwait(false);

        return new UpdateUserRoleResponse(createdUserRoleId.Value);
    }
}
