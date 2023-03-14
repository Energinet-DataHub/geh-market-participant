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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class GetUserRolesToPermissionHandler
    : IRequestHandler<GetUserRolesToPermissionCommand, GetUserRolesToPermissionResponse>
{
    private readonly IUserRoleRepository _userRoleRepository;

    public GetUserRolesToPermissionHandler(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task<GetUserRolesToPermissionResponse> Handle(
        GetUserRolesToPermissionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userRoles = new List<UserRoleDto>();

        foreach (var userRole in await _userRoleRepository.GetAsync((Permission)request.PermissionId).ConfigureAwait(false))
        {
            userRoles.Add(new UserRoleDto(
                userRole.Id.Value,
                userRole.Name,
                userRole.Name,
                userRole.EicFunction,
                userRole.Status));
        }

        return new GetUserRolesToPermissionResponse(userRoles);
    }
}
