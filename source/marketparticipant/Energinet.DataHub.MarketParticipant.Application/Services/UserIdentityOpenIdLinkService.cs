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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class UserIdentityOpenIdLinkService : IUserIdentityOpenIdLinkService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly ILogger<UserIdentityOpenIdLinkService> _logger;

    public UserIdentityOpenIdLinkService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        ILogger<UserIdentityOpenIdLinkService> logger)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _logger = logger;
    }

    public async Task<UserIdentity> ValidateAndSetupOpenIdAsync(Guid requestExternalUserId)
    {
        // if no user found .. THEN => {
        // Call service to get ad user from external user id
        // IsMember, MitID/openid
        var identityUser = await _userIdentityRepository
            .FindIdentityReadyForOpenIdSetupAsync(new ExternalUserId(requestExternalUserId)).ConfigureAwait(false);

        // User should have MitId, if not return unauthorized
        if (identityUser == null)
            throw new UnauthorizedAccessException($"External user id {requestExternalUserId} not found for open id setup.");

        // Ok, find user created in user flow by email with emailAddress signInType.
        var userIdentityInvitedOnEmail =
            await _userIdentityRepository.GetAsync(identityUser.Email).ConfigureAwait(false);

        if (userIdentityInvitedOnEmail == null)
            throw new NotFoundValidationException($"User with email {identityUser.Email} not found.");

        // Validate user time for MitId
        var userLocalIdentityEmail =
            await _userRepository.GetAsync(userIdentityInvitedOnEmail.Id).ConfigureAwait(false);

        if (userLocalIdentityEmail == null)
            throw new NotFoundValidationException($"User with id {userIdentityInvitedOnEmail.Id} not found.");

        if (userLocalIdentityEmail.MitIdSignupInitiatedAt < DateTime.UtcNow.AddMinutes(-300))
            throw new UnauthorizedAccessException($"OpenId signup initiated at {userLocalIdentityEmail.MitIdSignupInitiatedAt} is expired.");

        // Move openid login identity to user created from userflow
        // Find current identities, add to list and update and set
        MoveOpenIdLoginIdentityToInvitedUser(identityUser, userIdentityInvitedOnEmail);

        if (userIdentityInvitedOnEmail.LoginIdentities == null)
            throw new NotSupportedException($"OpenID login identity not found for user with id {userIdentityInvitedOnEmail.Id}.");

        await _userIdentityRepository
            .UpdateUserLoginIdentitiesAsync(userIdentityInvitedOnEmail.Id, userIdentityInvitedOnEmail.LoginIdentities)
            .ConfigureAwait(false);

        // Delete MitID user
        await _userIdentityRepository.DeleteUserIdentityAsync(identityUser.Id).ConfigureAwait(false);

        return userIdentityInvitedOnEmail;
    }

    private static void MoveOpenIdLoginIdentityToInvitedUser(
        UserIdentity openIdUserIdUserIdentity,
        UserIdentity invitedUserIdentity)
    {
        var loginIdentityToMove = openIdUserIdUserIdentity.LoginIdentities?.First(e => e.SignInType == "federated");

        if (loginIdentityToMove == null)
            throw new NotFoundValidationException($"OpenId login identity not found for user with id {openIdUserIdUserIdentity.Id}.");

        invitedUserIdentity.LoginIdentities?.Add(loginIdentityToMove);
    }
}
