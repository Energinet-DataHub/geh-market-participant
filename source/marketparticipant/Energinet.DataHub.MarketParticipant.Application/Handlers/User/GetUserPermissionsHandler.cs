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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetUserPermissionsHandler
    : IRequestHandler<GetUserPermissionsCommand, GetUserPermissionsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryRepository _userQueryRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public GetUserPermissionsHandler(
        IUserRepository userRepository,
        IUserQueryRepository userQueryRepository,
        IUserIdentityRepository userIdentityRepository)
    {
        _userRepository = userRepository;
        _userQueryRepository = userQueryRepository;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task<GetUserPermissionsResponse> Handle(
        GetUserPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new ExternalUserId(request.ExternalUserId))
            .ConfigureAwait(false);

        if (user == null)
        {
            var userIdentity = await ValidateAndSetupOpenIdAsync(request.ExternalUserId).ConfigureAwait(false);

            user = await _userRepository
                .GetAsync(new ExternalUserId(userIdentity.Id.Value))
                .ConfigureAwait(false);
        }

        var permissions = await _userQueryRepository
            .GetPermissionsAsync(new ActorId(request.ActorId), user.ExternalId)
            .ConfigureAwait(false);

        var isFas = await _userQueryRepository
            .IsFasAsync(new ActorId(request.ActorId), user.ExternalId)
            .ConfigureAwait(false);

        return new GetUserPermissionsResponse(user.Id.Value, isFas, permissions.Select(permission => permission.Claim));
    }

    private async Task<UserIdentity> ValidateAndSetupOpenIdAsync(Guid requestExternalUserId)
    {
        // if no user found .. THEN => {
        // Call service to get ad user from external user id
        // IsMember, MitID/openid
        var identityUser = await _userIdentityRepository.FindIdentityReadyForOpenIdSetupAsync(new ExternalUserId(requestExternalUserId)).ConfigureAwait(false);

        // User should have MitId, if not return unauthorized
        if (identityUser == null)
            throw new UnauthorizedAccessException($"External user id {requestExternalUserId} not found for open id setup.");

        // Ok, find user created in user flow by email with emailAddress signInType.
        var userIdentityInvitedOnEmail = await _userIdentityRepository.GetAsync(identityUser.Email).ConfigureAwait(false);

        if (userIdentityInvitedOnEmail == null)
            throw new NotFoundValidationException($"User with email {identityUser.Email} not found.");

        // Validate user time for MitId
        var userLocalIdentityEmail = await _userRepository.GetAsync(userIdentityInvitedOnEmail.Id).ConfigureAwait(false);

        if (userLocalIdentityEmail == null)
            throw new NotFoundValidationException($"User with id {userIdentityInvitedOnEmail.Id} not found.");

        if (userLocalIdentityEmail.MitIdSignupInitiatedAt < DateTime.UtcNow.AddMinutes(-300))
            throw new UnauthorizedAccessException($"OpenId signup initiated at {userLocalIdentityEmail.MitIdSignupInitiatedAt} is expired.");

        // Move login login methode to user created from userflow
        await _userIdentityRepository.UpdateUserSignInIdentitiesAsync(userIdentityInvitedOnEmail, "openIdSignIn").ConfigureAwait(false);

        // Delete MitID user
        await _userIdentityRepository.DeleteUserIdentityAsync(identityUser.Id).ConfigureAwait(false);

        return userIdentityInvitedOnEmail;
    }
}
