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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class CreateUserRoleHandler
    : IRequestHandler<CreateUserRoleCommand, CreateUserRoleResponse>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRoleAuditLogService _userRoleAuditLogService;
    private readonly IUserRoleAuditLogEntryRepository _userRoleAuditLogEntryRepository;
    private readonly IEnsureUserRolePermissionsService _ensureUserRolePermissionsService;

    public CreateUserRoleHandler(
        IUserRoleRepository userRoleRepository,
        IUserRoleAuditLogService userRoleAuditLogService,
        IUserRoleAuditLogEntryRepository userRoleAuditLogEntryRepository,
        IEnsureUserRolePermissionsService userRoleHelperService)
    {
        _userRoleRepository = userRoleRepository;
        _userRoleAuditLogService = userRoleAuditLogService;
        _userRoleAuditLogEntryRepository = userRoleAuditLogEntryRepository;
        _ensureUserRolePermissionsService = userRoleHelperService;
    }

    public async Task<CreateUserRoleResponse> Handle(
        CreateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var valid = await _ensureUserRolePermissionsService
            .EnsurePermissionsSelectedAreValidForMarketRoleAsync(
                request.UserRoleDto.Permissions.Select(x => (PermissionId)x),
                request.UserRoleDto.EicFunction)
            .ConfigureAwait(false);

        if (!valid)
            throw new ValidationException($"User role with name {request.UserRoleDto.Name} has permissions which are not valid for the market role selected {request.UserRoleDto.EicFunction}");

        var userRole = new UserRole(
            request.UserRoleDto.Name,
            request.UserRoleDto.Description,
            request.UserRoleDto.Status,
            request.UserRoleDto.Permissions.Select(x => (PermissionId)x),
            request.UserRoleDto.EicFunction,
            request.EditingUserId);

        var createdUserRoleId = await _userRoleRepository
            .AddAsync(userRole)
            .ConfigureAwait(false);

        var auditLogs = _userRoleAuditLogService.BuildAuditLogsForUserRoleCreated(new UserId(request.EditingUserId), createdUserRoleId, userRole);
        await _userRoleAuditLogEntryRepository.InsertAuditLogEntriesAsync(auditLogs).ConfigureAwait(false);

        return new CreateUserRoleResponse(createdUserRoleId.Value);
    }
}
