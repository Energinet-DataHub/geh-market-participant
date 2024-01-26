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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class PermissionRelationService : IPermissionRelationService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public PermissionRelationService(
        IPermissionRepository permissionRepository,
        IUserRoleRepository userRoleRepository)
    {
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<IEnumerable<PermissionRelationRecord>> BuildRelationRecordsAsync()
    {
        var allPermissions = await _permissionRepository.GetAllAsync().ConfigureAwait(false);

        var allUserRoles = (await _userRoleRepository.GetAllAsync().ConfigureAwait(false)).ToList();

        var allMarketRoles = Enum.GetNames<EicFunction>();

        var records = new List<PermissionRelationRecord>();

        foreach (var permission in allPermissions)
        {
            var userRoles = allUserRoles.Where(x => x.Permissions.Contains(permission.Id)).ToList();

            if (userRoles.Any())
            {
                foreach (var userRole in userRoles)
                {
                    records.Add(new PermissionRelationRecord(permission.Name, userRole.EicFunction.ToString(), userRole.Name));
                }
            }
            else
            {
                records.Add(new PermissionRelationRecord(permission.Name, string.Empty, string.Empty));
            }
        }

        foreach (var marketRole in allMarketRoles)
        {
            if (records.All(x => x.MarketRole != marketRole))
            {
                records.Add(new PermissionRelationRecord(string.Empty, marketRole, string.Empty));
            }
        }

        return records;
    }
}
