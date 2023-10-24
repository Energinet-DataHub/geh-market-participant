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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class CheckEmailExistsHandler : IRequestHandler<CheckEmailExistsCommand, bool>
{
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActorRepository _actorRepository;

    public CheckEmailExistsHandler(
        IUserContext<FrontendUser> userContext,
        IUserIdentityRepository userIdentityRepository,
        IUserRepository userRepository,
        IActorRepository actorRepository)
    {
        _userContext = userContext;
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _actorRepository = actorRepository;
    }

    public async Task<bool> Handle(CheckEmailExistsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userIdentity = await _userIdentityRepository
            .GetAsync(new EmailAddress(request.EmailAddress))
            .ConfigureAwait(false);

        if (userIdentity == null) return false;

        var user = await _userRepository
            .GetAsync(userIdentity.Id)
            .ConfigureAwait(false);

        if (user == null) return false;

        if (_userContext.CurrentUser.IsFas) return true;

        var actor = await _actorRepository
            .GetAsync(user.AdministratedBy)
            .ConfigureAwait(false);

        if (actor == null) return false;

        return actor.OrganizationId.Value == _userContext.CurrentUser.OrganizationId;
    }
}
