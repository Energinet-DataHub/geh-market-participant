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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class DeactivateUserRoleHandler
    : IRequestHandler<DeactivateUserRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IUserRoleAuditLogService _userRoleAuditLogService;
    private readonly IUserRoleAuditLogEntryRepository _userRoleAuditLogEntryRepository;
    private readonly IUserContext<FrontendUser> _userContext;

    public DeactivateUserRoleHandler(
        IUserRepository userRepository,
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository,
        IUserContext<FrontendUser> userContext,
        IUserRoleRepository userRoleRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IUserRoleAuditLogService userRoleAuditLogService,
        IUserRoleAuditLogEntryRepository userRoleAuditLogEntryRepository)
    {
        _userRepository = userRepository;
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
        _userContext = userContext;
        _userRoleRepository = userRoleRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _userRoleAuditLogService = userRoleAuditLogService;
        _userRoleAuditLogEntryRepository = userRoleAuditLogEntryRepository;
    }

    public async Task<Unit> Handle(
        DeactivateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userRoleId = new UserRoleId(request.UserRoleId);
        var userRole = await _userRoleRepository.GetAsync(userRoleId).ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(userRole, $"User role with id: {userRoleId} was not found");

        var users = await _userRepository
            .GetToUserRoleAsync(userRoleId)
            .ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            foreach (var user in users)
            {
                var role = user.RoleAssignments.Single(x => x.UserRoleId == userRoleId);
                user.RoleAssignments.Remove(role);

                await AuditRoleAssignmentAsync(user, role, UserRoleAssignmentTypeAuditLog.RemovedDueToDeactivation)
                    .ConfigureAwait(false);
                await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);
            }

            var userRoleInitStateForAuditLog = CopyUserRoleForAuditLog(userRole);

            userRole.Status = UserRoleStatus.Inactive;
            await _userRoleRepository.UpdateAsync(userRole).ConfigureAwait(false);

            var auditLogs = _userRoleAuditLogService.BuildAuditLogsForUserRoleChanged(
                new UserId(request.ChangedByUserId),
                userRoleInitStateForAuditLog,
                userRole);

            await _userRoleAuditLogEntryRepository.InsertAuditLogEntriesAsync(auditLogs).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }

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

    private async Task AuditRoleAssignmentAsync(
        Domain.Model.Users.User user,
        UserRoleAssignment userRoleAssignment,
        UserRoleAssignmentTypeAuditLog userRoleAssignmentTypeAuditLog)
    {
        await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
            user.Id,
            new UserRoleAssignmentAuditLogEntry(
                user.Id,
                userRoleAssignment.ActorId,
                userRoleAssignment.UserRoleId,
                new UserId(_userContext.CurrentUser.UserId),
                DateTimeOffset.UtcNow,
                userRoleAssignmentTypeAuditLog)).ConfigureAwait(false);
    }
}
