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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class GetAllUserRolesHandler
    : IRequestHandler<GetAllUserRolesCommand, GetAllUserRolesResponse>
{
    private readonly IUserRoleRepository _userRoleRepository;

    public GetAllUserRolesHandler(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task<GetAllUserRolesResponse> Handle(
        GetAllUserRolesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRoles = await _userRoleRepository
            .GetAllAsync()
            .ConfigureAwait(false);

        var userRolesList = userRoles.Select(role => new UserRoleInfoDto(role.Id.Value, role.Name, role.Description, role.EicFunction, role.Status));

        return new GetAllUserRolesResponse(userRolesList);
    }
}
