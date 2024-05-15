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
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class GetActorsAssociatedWithExternalUserIdHandler
    : IRequestHandler<GetActorsAssociatedWithExternalUserIdCommand, GetActorsAssociatedWithExternalUserIdResponse>
{
    private readonly IUserQueryRepository _userQueryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public GetActorsAssociatedWithExternalUserIdHandler(
        IUserQueryRepository userQueryRepository,
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository)
    {
        _userQueryRepository = userQueryRepository;
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task<GetActorsAssociatedWithExternalUserIdResponse> Handle(
        GetActorsAssociatedWithExternalUserIdCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var externalUserId = new ExternalUserId(request.ExternalUserId);
        var actorIds = (await _userQueryRepository
            .GetActorsAsync(externalUserId)
            .ConfigureAwait(false))
            .ToList();

        if (actorIds.Count != 0)
            return new GetActorsAssociatedWithExternalUserIdResponse(actorIds.Select(id => id.Value));

        var existingUser = await _userRepository
            .GetAsync(externalUserId)
            .ConfigureAwait(false);

        // If there are no associated actors found for an external user, there are three possibilities:
        // 1) User lost access to all actors, because all user role assignments were removed.
        if (existingUser != null)
            return new GetActorsAssociatedWithExternalUserIdResponse([]);

        // 2) User is an unlinked OpenId identity, calling first time after sign-in.
        var linkedIdentity = await GetActorsAssociatedWithOpenIdAsync(externalUserId).ConfigureAwait(false);
        if (linkedIdentity != null)
        {
            var linkedActorIds = await _userQueryRepository
                 .GetActorsAsync(linkedIdentity)
                 .ConfigureAwait(false);

            return new GetActorsAssociatedWithExternalUserIdResponse(linkedActorIds.Select(id => id.Value));
        }

        // 3) User is completely unknown.
        return new GetActorsAssociatedWithExternalUserIdResponse([]);
    }

    private async Task<ExternalUserId?> GetActorsAssociatedWithOpenIdAsync(ExternalUserId externalUserId)
    {
        var openIdIdentity = await _userIdentityRepository
            .FindIdentityReadyForOpenIdSetupAsync(externalUserId)
            .ConfigureAwait(false);

        if (openIdIdentity == null)
            return null;

        var linkedIdentity = await _userIdentityRepository
            .GetAsync(openIdIdentity.Email)
            .ConfigureAwait(false);

        if (linkedIdentity == null)
            return null;

        return linkedIdentity.Id;
    }
}
