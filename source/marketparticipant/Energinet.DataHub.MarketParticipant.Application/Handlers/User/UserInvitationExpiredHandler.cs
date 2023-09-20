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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class UserInvitationExpiredHandler : IRequestHandler<UserInvitationExpiredCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly ILogger<UserInvitationExpiredHandler> _logger;

    public UserInvitationExpiredHandler(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        ILogger<UserInvitationExpiredHandler> logger)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _logger = logger;
    }

    public async Task Handle(UserInvitationExpiredCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usersWithExpiredInvitation = await _userRepository.FindUsersWithExpiredInvitationAsync().ConfigureAwait(false);

        foreach (var user in usersWithExpiredInvitation)
        {
            await _userIdentityRepository.DisableUserAccountAsync(user.ExternalId).ConfigureAwait(false);
            _logger.LogInformation("User identity disabled for user with external id {ExternalId}", user.ExternalId);
        }
    }
}
