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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class UpdateUserRolesCommandHandler
    : IRequestHandler<UpdateUserRoleAssignmentsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;
    private readonly IUserContext<FrontendUser> _userContext;

    public UpdateUserRolesCommandHandler(
        IUserRepository userRepository,
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository,
        IUserContext<FrontendUser> userContext)
    {
        _userRepository = userRepository;
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        UpdateUserRoleAssignmentsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new NotFoundValidationException(request.UserId);
        }

        var (removedRoles, addedRoles) = FindUserRoleAssignmentChanges(request, user);

        ClearUserRolesForActorBeforeUpdate(request, user);

        foreach (var userRoleId in request.UserRoleAssignments)
        {
            user.RoleAssignments.Add(new UserRoleAssignment(
                user.Id,
                request.ActorId,
                new UserRoleId(userRoleId)));
        }

        await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);

        await CreateLogEntriesAsync(
            user.Id,
            new ExternalUserId(_userContext.CurrentUser.ExternalUserId),
            request.ActorId,
            removedRoles,
            addedRoles).ConfigureAwait(false);

        return Unit.Value;
    }

    private static (IEnumerable<Guid> RemovedUserRoles, IEnumerable<Guid> AddedUserRoles) FindUserRoleAssignmentChanges(UpdateUserRoleAssignmentsCommand request, Domain.Model.Users.User user)
    {
        var removedRoles = user.RoleAssignments
            .Where(a => a.ActorId == request.ActorId)
            .Select(e => e.UserRoleId.Value)
            .Except(request.UserRoleAssignments)
            .ToList();

        var addedRoles = request.UserRoleAssignments
            .Except(user.RoleAssignments
                .Where(a => a.ActorId == request.ActorId)
                .Select(e => e.UserRoleId.Value))
            .ToList();

        return (removedRoles, addedRoles);
    }

    private static void ClearUserRolesForActorBeforeUpdate(UpdateUserRoleAssignmentsCommand request, Domain.Model.Users.User user)
    {
        foreach (var userRoleAssignment in user.RoleAssignments.Where(e => e.ActorId == request.ActorId).ToList())
        {
            user.RoleAssignments.Remove(userRoleAssignment);
        }
    }

    private async Task CreateLogEntriesAsync(
        UserId userId,
        ExternalUserId changedByUserId,
        Guid actorId,
        IEnumerable<Guid> removedUserRoles,
        IEnumerable<Guid> addedUserRoles)
    {
        foreach (var userRoleId in addedUserRoles)
        {
            await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
                new UserRoleAssignmentAuditLogEntry(
                    userId,
                    actorId,
                    new UserRoleId(userRoleId),
                    changedByUserId,
                    DateTimeOffset.UtcNow,
                    UserRoleAssignmentTypeAuditLog.Added)).ConfigureAwait(false);
        }

        foreach (var userRoleId in removedUserRoles)
        {
            await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
                new UserRoleAssignmentAuditLogEntry(
                    userId,
                    actorId,
                    new UserRoleId(userRoleId),
                    changedByUserId,
                    DateTimeOffset.UtcNow,
                    UserRoleAssignmentTypeAuditLog.Removed)).ConfigureAwait(false);
        }
    }
}
