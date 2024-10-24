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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class GetUserHandler : IRequestHandler<GetUserCommand, GetUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserStatusCalculator _userStatusCalculator;

    public GetUserHandler(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserStatusCalculator userStatusCalculator)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userStatusCalculator = userStatusCalculator;
    }

    public async Task<GetUserResponse> Handle(GetUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(user, request.UserId);

        var externalIdentities = await _userIdentityRepository
            .GetUserIdentitiesAsync(new[] { user.ExternalId })
            .ConfigureAwait(false);

        var externalIdentity = externalIdentities.SingleOrDefault();
        NotFoundValidationException.ThrowIfNull(externalIdentity, user.Id.Value, $"No external identity found for id {user.Id}.");

        return new GetUserResponse(
            user.Id.Value,
            externalIdentity.FullName,
            _userStatusCalculator.CalculateUserStatus(externalIdentity.Status, user.InvitationExpiresAt),
            externalIdentity.FirstName,
            externalIdentity.LastName,
            externalIdentity.Email.Address,
            externalIdentity.PhoneNumber?.Number,
            user.AdministratedBy.Value,
            externalIdentity.CreatedDate,
            user.LatestLoginAt);
    }
}
