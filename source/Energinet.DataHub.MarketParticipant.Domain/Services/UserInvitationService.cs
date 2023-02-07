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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

// TODO: UTs, Registration
public sealed class UserInvitationService : IUserInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IActorQueryRepository _actorQueryRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public UserInvitationService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IOrganizationRepository organizationRepository,
        IActorQueryRepository actorQueryRepository,
        IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _organizationRepository = organizationRepository;
        _actorQueryRepository = actorQueryRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task InviteUserAsync(UserInvitation invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        await VerifyAsync(invitation).ConfigureAwait(false);

        // TODO: Is this correct? Should I look up in AD to get external id?
        var invitedUser = await _userRepository
            .GetAsync(invitation.Email)
            .ConfigureAwait(false);

        if (invitedUser == null)
        {
            var userIdentity = new UserIdentity(
                invitation.Email,
                invitation.FirstName,
                invitation.LastName,
                invitation.PhoneNumber,
                invitation.RequiredAuthentication);

            var userIdentityId = await _userIdentityRepository
                .CreateAsync(userIdentity)
                .ConfigureAwait(false);

            invitedUser = new User(userIdentityId, invitation.Email);
        }

        // TODO: Audit log user.

        // TODO: Audit log this.
        foreach (var assignedRole in invitation.AssignedRoles)
        {
            invitedUser.RoleAssignments.Add(new UserRoleAssignment(invitation.AssignedActor, assignedRole));
        }

        await _userRepository
            .AddOrUpdateAsync(invitedUser)
            .ConfigureAwait(false);
    }

    private async Task VerifyAsync(UserInvitation invitation)
    {
        var actor = await _actorQueryRepository
            .GetActorAsync(invitation.AssignedActor)
            .ConfigureAwait(false);

        if (actor is null)
        {
            throw new NotFoundValidationException($"The specified actor {invitation.AssignedActor} was not found.");
        }

        // TODO: What about passive? What about users when actor becomes passive? What about the race condition?
        if (actor is not { Status: ActorStatus.Active })
        {
            throw new ValidationException("The specified actor has an incorrect state.");
        }

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        if (organization is not { Status: OrganizationStatus.Active })
        {
            throw new ValidationException("The organization of the actor has an incorrect state.");
        }

        var actorFunction = organization
            .Actors
            .Single(a => a.Id == actor.ActorId)
            .MarketRoles
            .Single();

        foreach (var userRoleId in invitation.AssignedRoles)
        {
            var userRole = await _userRoleRepository
                .GetAsync(userRoleId)
                .ConfigureAwait(false);

            if (userRole is null)
            {
                throw new NotFoundValidationException($"The specified user role {userRoleId} was not found.");
            }

            // TODO: Is this correct? What about race condition?
            if (userRole.Status != UserRoleStatus.Active)
            {
                throw new ValidationException($"The specified user role {userRoleId} has an incorrect state.");
            }

            // TODO: Do we have this rule in a common place?
            if (userRole.EicFunction != actorFunction.Function)
            {
                throw new ValidationException($"The specified user role {userRoleId} cannot be used with the specified actor {actor.ActorId}.");
            }
        }
    }
}
