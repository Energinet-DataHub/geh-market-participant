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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

public sealed class UniqueUserRoleNameRuleService : IUniqueUserRoleNameRuleService
{
    private readonly IUserRoleRepository _userRoleRepository;

    public UniqueUserRoleNameRuleService(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task ValidateUserRoleNameAsync(UserRole userRole)
    {
        ArgumentNullException.ThrowIfNull(userRole);

        var userRolesWithSameName = await _userRoleRepository
            .GetByNameInMarketRoleAsync(userRole.Name, userRole.EicFunction)
            .ConfigureAwait(false);

        var activeUserRolesWithSameName = userRolesWithSameName
            .Where(role => role.Status == UserRoleStatus.Active)
            .Where(role => role.Id != userRole.Id);

        if (activeUserRolesWithSameName.Any())
        {
            throw new ValidationException($"Requested user role name '{userRole.Name}' is already in use.")
                .WithErrorCode("market_role.reserved");
        }
    }
}
