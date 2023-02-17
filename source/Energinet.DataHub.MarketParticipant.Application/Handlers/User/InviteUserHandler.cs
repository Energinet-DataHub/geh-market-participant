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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class InviteUserHandler : IRequestHandler<InviteUserCommand>
{
    private readonly IUserInvitationService _userInvitationService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IActorQueryRepository _actorQueryRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public InviteUserHandler(
        IUserInvitationService userInvitationService,
        IOrganizationRepository organizationRepository,
        IActorQueryRepository actorQueryRepository,
        IUserRoleRepository userRoleRepository)
    {
        _userInvitationService = userInvitationService;
        _organizationRepository = organizationRepository;
        _actorQueryRepository = actorQueryRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Unit> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assignedActor = await GetActorAsync(request.Invitation.AssignedActor).ConfigureAwait(false);
        var assignedRoles = await GetUserRolesAsync(request.Invitation.AssignedRoles).ConfigureAwait(false);

        var phoneNumber = new PhoneNumber(request.Invitation.PhoneNumber);

        var invitation = new UserInvitation(
            new EmailAddress(request.Invitation.Email),
            request.Invitation.FirstName,
            request.Invitation.LastName,
            phoneNumber,
            new SmsAuthenticationMethod(phoneNumber),
            assignedActor,
            assignedRoles);

        await _userInvitationService
            .InviteUserAsync(invitation)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task<Domain.Model.Actor> GetActorAsync(Guid actorId)
    {
        var actor = await _actorQueryRepository
            .GetActorAsync(actorId)
            .ConfigureAwait(false);

        if (actor == null)
        {
            throw new NotFoundValidationException($"The specified actor {actorId} was not found.");
        }

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        return organization != null && organization.Status != OrganizationStatus.Deleted
            ? organization.Actors.First(a => a.Id == actor.ActorId)
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

            if (userRole == null)
            {
                throw new NotFoundValidationException($"The specified user role {userRole} was not found.");
            }

            assignedRoles.Add(userRole);
        }

        return assignedRoles;
    }
}
