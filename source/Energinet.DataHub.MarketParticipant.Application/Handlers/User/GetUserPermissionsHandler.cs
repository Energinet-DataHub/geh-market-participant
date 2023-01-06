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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
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
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserQueryRepository _userQueryRepository;

    public GetUserPermissionsHandler(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserQueryRepository userQueryRepository)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userQueryRepository = userQueryRepository;
    }

    // This is a temporary workaround that creates users from AD/B2C in our own database.
    // TODO: Remove once invite flow has been implemented.
    public static Func<IEnumerable<ExternalUserId>, Task<IEnumerable<UserIdentity>>>? TestWorkaround { get; set; }

    public async Task<GetUserPermissionsResponse> Handle(
        GetUserPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new ExternalUserId(request.ExternalUserId))
            .ConfigureAwait(false);

        // This is a temporary workaround that creates users from AD/B2C in our own database.
        // TODO: Remove once invite flow has been implemented.
        if (user == null)
        {
            user = await CreateFallbackUserAsync(new ExternalUserId(request.ExternalUserId)).ConfigureAwait(false);
        }

        var permissions = await _userQueryRepository
            .GetPermissionsAsync(request.ActorId, user.ExternalId)
            .ConfigureAwait(false);

        var isFas = await _userQueryRepository
            .IsFasAsync(request.ActorId, user.ExternalId)
            .ConfigureAwait(false);

        return new GetUserPermissionsResponse(user.Id.Value, isFas, permissions);
    }

    private async Task<Domain.Model.Users.User> CreateFallbackUserAsync(ExternalUserId externalUserId)
    {
        UserIdentity userIdentity;

        if (TestWorkaround != null)
        {
            var userIdentities = await TestWorkaround(new[] { externalUserId }).ConfigureAwait(false);
            userIdentity = userIdentities.Single();
        }
        else
        {
            var userIdentities = await _userIdentityRepository
                .GetUserIdentitiesAsync(new[] { externalUserId })
                .ConfigureAwait(false);

            userIdentity = userIdentities.Single();
        }

        var user = new Domain.Model.Users.User(
            externalUserId,
            new List<UserRoleAssignment>(),
            userIdentity.Email);

        await _userRepository
            .AddOrUpdateAsync(user)
            .ConfigureAwait(false);

        return (await _userRepository
            .GetAsync(externalUserId)
            .ConfigureAwait(false))!;
    }
}
