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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetAvailableUserRolesForActorHandler
    : IRequestHandler<GetAvailableUserRolesForActorCommand, GetUserRolesResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetAvailableUserRolesForActorHandler(
        IActorRepository actorRepository,
        IUserRoleRepository userRoleRepository)
    {
        _actorRepository = actorRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<GetUserRolesResponse> Handle(
        GetAvailableUserRolesForActorCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        var eicFunctions = actor
            .MarketRoles
            .Select(r => r.Function);

        var userRoles = await _userRoleRepository
            .GetAsync(eicFunctions)
            .ConfigureAwait(false);

        return new GetUserRolesResponse(userRoles
            .Where(u => u.Status == UserRoleStatus.Active)
            .Select(t => new UserRoleDto(
                t.Id.Value,
                t.Name,
                t.Description,
                t.EicFunction,
                t.Status)));
    }
}
