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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;

public sealed class GetPermissionsRelationHandler
    : IRequestHandler<GetPermissionsRelationCommand, Stream>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetPermissionsRelationHandler(
        IPermissionRepository permissionRepository,
        IUserRoleRepository userRoleRepository)
    {
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Stream> Handle(GetPermissionsRelationCommand request, CancellationToken cancellationToken)
    {
        var allPermissions = await _permissionRepository.GetAllAsync().ConfigureAwait(false);

        var allUserRoles = (await _userRoleRepository.GetAllAsync().ConfigureAwait(false)).ToList();

        var allMarketRoles = Enum.GetNames<EicFunction>();

        var stringWriter = new StringBuilder();
        stringWriter.AppendLine("PermissionName;UserRoleName;MarketRole");

        foreach (var permission in allPermissions.OrderBy(e => e.Name))
        {
            var userRoles = allUserRoles.Where(x => x.Permissions.Contains(permission.Id)).ToList();

            if (userRoles.Any())
            {
                foreach (var userRole in userRoles)
                {
                    var line = $"{permission.Name};{userRole.Name};{userRole.EicFunction}";
                    stringWriter.AppendLine(line);
                }
            }
            else
            {
                var line = $"{permission.Name};;";
                stringWriter.AppendLine(line);
            }
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(stringWriter.ToString()));
        return stream;
    }
}
