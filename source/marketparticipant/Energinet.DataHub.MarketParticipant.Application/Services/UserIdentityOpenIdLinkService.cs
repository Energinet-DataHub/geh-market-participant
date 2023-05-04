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
        var identityUserOpenId = await _userIdentityRepository.FindIdentityReadyForOpenIdSetupAsync(new ExternalUserId(requestExternalUserId)).ConfigureAwait(false);

        if (identityUserOpenId == null)
            throw new UnauthorizedAccessException($"External user id {requestExternalUserId} not found for open id setup.");

        var userIdentityInvitedOnEmail = await _userIdentityRepository.GetAsync(identityUserOpenId.Email).ConfigureAwait(false);

        if (userIdentityInvitedOnEmail == null)
            throw new NotFoundValidationException($"User with email {identityUserOpenId.Email} not found with expected signInType.");

        var userLocalIdentityByEmail = await _userRepository.GetAsync(userIdentityInvitedOnEmail.Id).ConfigureAwait(false);

        if (userLocalIdentityByEmail == null)
            throw new NotFoundValidationException($"User with id {userIdentityInvitedOnEmail.Id} not found.");

        if (userLocalIdentityByEmail.MitIdSignupInitiatedAt < DateTime.UtcNow.AddMinutes(-300))
            throw new UnauthorizedAccessException($"OpenId signup initiated at {userLocalIdentityByEmail.MitIdSignupInitiatedAt} is expired.");

        MoveOpenIdLoginIdentityToInvitedUser(identityUserOpenId, userIdentityInvitedOnEmail);

        if (userIdentityInvitedOnEmail.LoginIdentities == null)
            throw new NotSupportedException($"OpenID login identity not found for user with id {userIdentityInvitedOnEmail.Id}.");

        await _userIdentityRepository.DeleteOpenIdUserIdentityAsync(identityUserOpenId.Id).ConfigureAwait(false);

        await _userIdentityRepository
            .UpdateUserLoginIdentitiesAsync(userIdentityInvitedOnEmail.Id, userIdentityInvitedOnEmail.LoginIdentities)
            .ConfigureAwait(false);

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
