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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class GetUserRolesHandler
    : IRequestHandler<GetUserRolesCommand, GetUserRolesResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetUserRolesHandler(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<GetUserRolesResponse> Handle(
        GetUserRolesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actorId = new ActorId(request.ActorId);
        var user = await _userRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(user, request.UserId);

        var assignments = user
            .RoleAssignments
            .Where(a => a.ActorId == actorId)
            .Select(x => x.UserRoleId)
            .Distinct();

        var userRoles = new List<UserRoleDto>();

        foreach (var assignment in assignments)
        {
            var userRole = await _userRoleRepository
                .GetAsync(assignment)
                .ConfigureAwait(false);

            if (userRole != null)
            {
                var role = new UserRoleDto(userRole.Id.Value, userRole.Name, userRole.Description, userRole.EicFunction, userRole.Status, userRole.ChangedByIdentityId);
                userRoles.Add(role);
            }
        }

        return new GetUserRolesResponse(userRoles);
    }
}
