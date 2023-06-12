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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class UserIdentityOpenIdLinkService : IUserIdentityOpenIdLinkService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public UserIdentityOpenIdLinkService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task<UserIdentity> ValidateAndSetupOpenIdAsync(ExternalUserId requestExternalUserId)
    {
        var identityUserOpenId = await _userIdentityRepository.FindIdentityReadyForOpenIdSetupAsync(requestExternalUserId).ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(identityUserOpenId, $"External user id {requestExternalUserId} not found for open id setup.");

        var userIdentityInvitedOnEmail = await _userIdentityRepository.GetAsync(identityUserOpenId.Email).ConfigureAwait(false);

        if (userIdentityInvitedOnEmail == null)
        {
            await DeleteOpenIdUserAsync(identityUserOpenId.Id).ConfigureAwait(false);
            throw new NotFoundValidationException($"User with email {identityUserOpenId.Email} not found with expected signInType.");
        }

        var userLocalIdentityByEmail = await _userRepository.GetAsync(userIdentityInvitedOnEmail.Id).ConfigureAwait(false);

        if (userLocalIdentityByEmail == null)
        {
            await DeleteOpenIdUserAsync(identityUserOpenId.Id).ConfigureAwait(false);
            throw new NotFoundValidationException($"User with id {userIdentityInvitedOnEmail.Id} not found.");
        }

        if (userLocalIdentityByEmail.MitIdSignupInitiatedAt < DateTimeOffset.UtcNow.AddMinutes(-15))
        {
            await DeleteOpenIdUserAsync(identityUserOpenId.Id).ConfigureAwait(false);
            throw new UnauthorizedAccessException($"OpenId signup initiated at {userLocalIdentityByEmail.MitIdSignupInitiatedAt} is expired.");
        }

        userIdentityInvitedOnEmail.LinkOpenIdFrom(identityUserOpenId);

        await DeleteOpenIdUserAsync(identityUserOpenId.Id).ConfigureAwait(false);

        await _userIdentityRepository
            .AssignUserLoginIdentitiesAsync(userIdentityInvitedOnEmail)
            .ConfigureAwait(false);

        return userIdentityInvitedOnEmail;
    }

    private Task DeleteOpenIdUserAsync(ExternalUserId externalUserId)
    {
        return _userIdentityRepository.DeleteAsync(externalUserId);
    }
}
