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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

public sealed class RequiredPermissionForUserRoleRuleService : IRequiredPermissionForUserRoleRuleService
{
    private static readonly HashSet<(PermissionId Permission, EicFunction MarketRole)> _requiredPermissions =
    [
        (PermissionId.UsersManage, EicFunction.DataHubAdministrator),
        (PermissionId.UserRolesManage, EicFunction.DataHubAdministrator),
    ];

    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public RequiredPermissionForUserRoleRuleService(IUserRoleRepository userRoleRepository, IUserRepository userRepository, IUserIdentityRepository userIdentityRepository)
    {
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task ValidateExistsAsync(IEnumerable<UserId> excludedUsers)
    {
        var excludedUsersLookup = excludedUsers.ToHashSet();

        foreach (var (permission, marketRole) in _requiredPermissions)
        {
            var userRoles = await _userRoleRepository.GetAsync(permission).ConfigureAwait(false);

            foreach (var userRole in userRoles)
            {
                if (userRole.EicFunction != marketRole)
                {
                    continue;
                }

                if (userRole.Status == UserRoleStatus.Inactive)
                {
                    continue;
                }

                var users = await _userRepository.GetToUserRoleAsync(userRole.Id).ConfigureAwait(false);

                foreach (var user in users)
                {
                    if (excludedUsersLookup.Contains(user.Id))
                    {
                        continue;
                    }

                    var userIdentity = await _userIdentityRepository.GetAsync(user.ExternalId).ConfigureAwait(false);

                    if (userIdentity is { Status: UserIdentityStatus.Active })
                    {
                        return;
                    }
                }
            }

            throw new ValidationException($"This operation would've removed the permission '{permission}' from the last remaining user role with active users for the market role '{marketRole}', and hence was denied.")
                .WithErrorCode("required_permission_removed")
                .WithArgs(("permission", permission), ("marketRole", marketRole));
        }
    }

    public Task ValidateExistsAsync()
    {
        return ValidateExistsAsync([]);
    }
}
