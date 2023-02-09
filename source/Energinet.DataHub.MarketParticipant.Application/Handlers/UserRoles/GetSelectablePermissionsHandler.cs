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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class GetSelectablePermissionsHandler
    : IRequestHandler<GetSelectablePermissionsCommand, GetSelectablePermissionsResponse>
{
    private readonly IPermissionRepository _permissionRepository;

    public GetSelectablePermissionsHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<GetSelectablePermissionsResponse> Handle(
        GetSelectablePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var permissions = await _permissionRepository.GetToMarketRoleAsync(request.EicFunction).ConfigureAwait(false);
        return new GetSelectablePermissionsResponse(permissions.Select(permission =>
            new SelectablePermissionDto(
                (int)permission.Permission,
                permission.Permission.ToString(),
                permission.Description)));
    }
}
