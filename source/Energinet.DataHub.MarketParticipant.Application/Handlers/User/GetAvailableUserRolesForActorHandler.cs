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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetAvailableUserRolesForActorHandler
    : IRequestHandler<GetAvailableUserRolesForActorCommand, GetUserRolesResponse>
{
    private readonly IActorQueryRepository _actorQueryRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetAvailableUserRolesForActorHandler(
        IActorQueryRepository actorQueryRepository,
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository)
    {
        _actorQueryRepository = actorQueryRepository;
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<GetUserRolesResponse> Handle(
        GetAvailableUserRolesForActorCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorQueryRepository
            .GetActorAsync(request.ActorId)
            .ConfigureAwait(false);

        if (actor == null)
        {
            throw new NotFoundValidationException(request.ActorId);
        }

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        var eicFunctions = organization!
            .Actors
            .Where(a => a.Id == actor.ActorId)
            .SelectMany(a => a.MarketRoles)
            .Select(r => r.Function);

        var userRoles = await _userRoleRepository
            .GetAsync(eicFunctions)
            .ConfigureAwait(false);

        return new GetUserRolesResponse(userRoles.Select(t =>
        {
            return new UserRoleDto(t.Id.Value, t.Name, t.Description, t.EicFunction, t.Status);
        }));
    }
}
