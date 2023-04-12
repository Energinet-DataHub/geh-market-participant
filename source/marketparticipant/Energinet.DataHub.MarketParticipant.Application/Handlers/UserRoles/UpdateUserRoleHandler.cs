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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand>
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

    public async Task<Unit> Handle(
        UpdateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRoleToUpdate = await _userRoleRepository.GetAsync(new UserRoleId(request.UserRoleId)).ConfigureAwait(false);
        if (userRoleToUpdate == null)
        {
            throw new NotFoundValidationException(request.UserRoleId);
        }

        if (userRoleToUpdate.Status == UserRoleStatus.Inactive)
            throw new ValidationException($"User role with name {request.UserRoleUpdateDto.Name} is deactivated and can't be updated");

        var userRoleWithSameName = await _userRoleRepository.GetByNameInMarketRoleAsync(request.UserRoleUpdateDto.Name, userRoleToUpdate.EicFunction).ConfigureAwait(false);
        if (userRoleWithSameName != null && userRoleWithSameName.Id.Value != userRoleToUpdate.Id.Value)
        {
            throw new ValidationException($"User role with name {request.UserRoleUpdateDto.Name} already exists in market role");
        }

        var userRoleInitStateForAuditLog = CopyUserRoleForAuditLog(userRoleToUpdate);

        userRoleToUpdate.Name = request.UserRoleUpdateDto.Name;
        userRoleToUpdate.Description = request.UserRoleUpdateDto.Description;
        userRoleToUpdate.Status = request.UserRoleUpdateDto.Status;
        userRoleToUpdate.Permissions = request.UserRoleUpdateDto.Permissions.Select(p => (PermissionId)p);

        await _userRoleRepository
            .UpdateAsync(userRoleToUpdate)
            .ConfigureAwait(false);

        var auditLogs = _userRoleAuditLogService.BuildAuditLogsForUserRoleChanged(new UserId(request.ChangedByUserId), userRoleInitStateForAuditLog, userRoleToUpdate);
        await _userRoleAuditLogEntryRepository.InsertAuditLogEntriesAsync(auditLogs).ConfigureAwait(false);

        return Unit.Value;
    }

    private static UserRole CopyUserRoleForAuditLog(UserRole userRole)
    {
        return new UserRole(
            userRole.Id,
            userRole.Name,
            userRole.Description,
            userRole.Status,
            userRole.Permissions,
            userRole.EicFunction);
    }
}
