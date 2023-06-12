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
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class GetUserRoleHandler
    : IRequestHandler<GetUserRoleCommand, GetUserRoleResponse>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public GetUserRoleHandler(IUserRoleRepository userRoleRepository, IPermissionRepository permissionRepository)
    {
        _userRoleRepository = userRoleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<GetUserRoleResponse> Handle(
        GetUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRole = await _userRoleRepository
            .GetAsync(new UserRoleId(request.UserRoleId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(userRole, request.UserRoleId);

        var permissionDetailsLookup = (await _permissionRepository
            .GetForMarketRoleAsync(userRole.EicFunction)
            .ConfigureAwait(false))
            .ToDictionary(x => x.Id);

        return new GetUserRoleResponse(new UserRoleWithPermissionsDto(
            userRole.Id.Value,
            userRole.Name,
            userRole.Description,
            userRole.EicFunction,
            userRole.Status,
            userRole.Permissions
                .Where(x => permissionDetailsLookup.ContainsKey(x))
                .Select(x => MapPermission(permissionDetailsLookup[x]))));
    }

    private static PermissionDetailsDto MapPermission(Permission permission)
    {
        return new PermissionDetailsDto(
            (int)permission.Id,
            permission.Name,
            permission.Description,
            permission.Created.ToDateTimeOffset());
    }
}
