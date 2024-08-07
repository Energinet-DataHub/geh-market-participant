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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class GetUserPermissionsHandler
    : IRequestHandler<GetUserPermissionsCommand, GetUserPermissionsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryRepository _userQueryRepository;
    private readonly IUserIdentityOpenIdLinkService _userIdentityOpenIdLinkService;
    private readonly IUserIdentityAuthenticationService _userIdentityAuthenticationService;
    private readonly ILogger<GetUserPermissionsHandler> _logger;

    public GetUserPermissionsHandler(
        IUserRepository userRepository,
        IUserQueryRepository userQueryRepository,
        IUserIdentityOpenIdLinkService userIdentityOpenIdLinkService,
        IUserIdentityAuthenticationService userIdentityAuthenticationService,
        ILogger<GetUserPermissionsHandler> logger)
    {
        _userRepository = userRepository;
        _userQueryRepository = userQueryRepository;
        _userIdentityOpenIdLinkService = userIdentityOpenIdLinkService;
        _userIdentityAuthenticationService = userIdentityAuthenticationService;
        _logger = logger;
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
            var userIdentity = await _userIdentityOpenIdLinkService
                .ValidateAndSetupOpenIdAsync(new ExternalUserId(request.ExternalUserId))
                .ConfigureAwait(false);

            user = await _userRepository
                .GetAsync(new ExternalUserId(userIdentity.Id.Value))
                .ConfigureAwait(false);
        }

        await ValidateOrClearLogonRequirementsAsync(user!).ConfigureAwait(false);

        var permissions = await _userQueryRepository
            .GetPermissionsAsync(new ActorId(request.ActorId), user!.ExternalId)
            .ConfigureAwait(false);

        var isFas = await _userQueryRepository
            .IsFasAsync(new ActorId(request.ActorId), user.ExternalId)
            .ConfigureAwait(false);

        return new GetUserPermissionsResponse(user.Id.Value, isFas, permissions.Select(permission => permission.Claim));
    }

    private async Task ValidateOrClearLogonRequirementsAsync(User user)
    {
        if (!user.ValidLogonRequirements)
        {
            throw new UnauthorizedAccessException("User invitation has expired");
        }

        if (user.InvitationExpiresAt.HasValue)
        {
            var waitTimeS = 1;

            var has2Fa = await _userIdentityAuthenticationService
                .HasTwoFactorAuthenticationAsync(user.ExternalId)
                .ConfigureAwait(false);

            while (!has2Fa && waitTimeS <= 4)
            {
                await Task.Delay(TimeSpan.FromSeconds(waitTimeS)).ConfigureAwait(false);

                has2Fa = await _userIdentityAuthenticationService
                    .HasTwoFactorAuthenticationAsync(user.ExternalId)
                    .ConfigureAwait(false);

                waitTimeS *= 2;
            }

            if (has2Fa)
            {
                user.DeactivateUserExpiration();
                await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("User {UserId} was logged in, but has not enabled 2FA. There may be a race condition?", user.Id.Value);
            }
        }
    }
}
