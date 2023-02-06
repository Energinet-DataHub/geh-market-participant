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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

// TODO: UTs, Registration
public sealed class UserInvitationService : IUserInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public UserInvitationService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task InviteUserAsync(UserInvitation invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);

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

        foreach (var assignedRole in invitation.AssignedRoles)
        {
            invitedUser.RoleAssignments.Add(new UserRoleAssignment(invitation.AssignedActor, assignedRole));
        }

        await _userRepository
            .AddOrUpdateAsync(invitedUser)
            .ConfigureAwait(false);
    }
}
