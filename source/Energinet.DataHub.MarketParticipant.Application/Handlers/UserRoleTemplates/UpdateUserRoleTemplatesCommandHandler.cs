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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoleTemplates;

public sealed class UpdateUserRoleTemplatesCommandHandler
    : IRequestHandler<UpdateUserRoleAssignmentsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;

    public UpdateUserRoleTemplatesCommandHandler(
        IUserRepository userRepository,
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository)
    {
        _userRepository = userRepository;
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
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

        foreach (var userRoleTemplateId in request.RoleAssignmentsDto.UserRoleTemplateAssignments)
        {
            user.RoleAssignments.Add(new UserRoleAssignment(
                user.Id,
                request.RoleAssignmentsDto.ActorId,
                userRoleTemplateId));
        }

        await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);

        await CreateLogEntriesAsync(user.Id, user.Id, request.RoleAssignmentsDto.ActorId, removedRoles, addedRoles).ConfigureAwait(false);

        return Unit.Value;
    }

    private static (IEnumerable<Guid> RemovedUserRoles, IEnumerable<Guid> AddedUserRoles) FindUserRoleAssignmentChanges(UpdateUserRoleAssignmentsCommand request, Domain.Model.Users.User user)
    {
        var userRolesIdsSelectedForActor = request.RoleAssignmentsDto.UserRoleTemplateAssignments.Select(r => r.Value).ToList();

        var removedRolesRoles = user.RoleAssignments
            .Where(a => a.ActorId == request.RoleAssignmentsDto.ActorId)
            .Select(e => e.TemplateId.Value)
            .Except(userRolesIdsSelectedForActor)
            .ToList();

        var addedRoles = userRolesIdsSelectedForActor
            .Except(user.RoleAssignments
                .Where(a => a.ActorId == request.RoleAssignmentsDto.ActorId)
                .Select(e => e.TemplateId.Value))
            .ToList();

        return (removedRolesRoles, addedRoles);
    }

    private static void ClearUserRolesForActorBeforeUpdate(UpdateUserRoleAssignmentsCommand request, Domain.Model.Users.User user)
    {
        foreach (var userRoleAssignment in user.RoleAssignments.Where(e => e.ActorId == request.RoleAssignmentsDto.ActorId).ToList())
        {
            user.RoleAssignments.Remove(userRoleAssignment);
        }
    }

    private async Task CreateLogEntriesAsync(UserId userId, UserId changedByUserId, Guid actorId, IEnumerable<Guid> removedUserRoles, IEnumerable<Guid> addedUserRoles)
    {
        foreach (var userRoleId in addedUserRoles)
        {
            await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
                new UserRoleAssignmentAuditLogEntry(
                    userId,
                    actorId,
                    new UserRoleTemplateId(userRoleId),
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
                    new UserRoleTemplateId(userRoleId),
                    changedByUserId,
                    DateTimeOffset.UtcNow,
                    UserRoleAssignmentTypeAuditLog.Removed)).ConfigureAwait(false);
        }
    }
}
