﻿// Copyright 2020 Energinet DataHub A/S
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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class InviteUserHandler : IRequestHandler<InviteUserCommand>
{
    private readonly IUserInvitationService _userInvitationService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public InviteUserHandler(
        IUserInvitationService userInvitationService,
        IOrganizationRepository organizationRepository,
        IActorRepository actorRepository,
        IUserRoleRepository userRoleRepository)
    {
        _userInvitationService = userInvitationService;
        _organizationRepository = organizationRepository;
        _actorRepository = actorRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assignedActor = await GetActorAsync(request.Invitation.AssignedActor).ConfigureAwait(false);
        var assignedRoles = await GetUserRolesAsync(request.Invitation.AssignedRoles).ConfigureAwait(false);

        var details = request.Invitation.InvitationUserDetails != null
            ? new InvitationUserDetails(
                request.Invitation.InvitationUserDetails.FirstName,
                request.Invitation.InvitationUserDetails.LastName,
                new PhoneNumber(request.Invitation.InvitationUserDetails.PhoneNumber),
                new SmsAuthenticationMethod(new PhoneNumber(request.Invitation.InvitationUserDetails.PhoneNumber)))
            : null;

        var invitation = new UserInvitation(
            new EmailAddress(request.Invitation.Email),
            details,
            assignedActor,
            assignedRoles);

        await _userInvitationService
            .InviteUserAsync(invitation, new UserId(request.InvitedByUserId))
            .ConfigureAwait(false);
    }

    private async Task<Actor> GetActorAsync(Guid actorId)
    {
        var actor = await _actorRepository
            .GetAsync(new ActorId(actorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, actorId);

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        return organization != null && organization.Status != OrganizationStatus.Deleted
            ? actor
            : throw new ValidationException("The organization of the actor has been deleted.");
    }

    private async Task<List<UserRole>> GetUserRolesAsync(IEnumerable<Guid> userRoleIds)
    {
        var assignedRoles = new List<UserRole>();

        foreach (var userRoleId in userRoleIds)
        {
            var userRole = await _userRoleRepository
                .GetAsync(new UserRoleId(userRoleId))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(userRole, userRoleId, $"The specified user role with id {userRoleId} was not found.");

            assignedRoles.Add(userRole);
        }

        return assignedRoles;
    }
}
