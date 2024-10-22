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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

/// <summary>
/// Validates that required permissions exists for at least one user role, which is assigned to at least one user.
/// </summary>
public interface IRequiredPermissionForUserRoleRuleService
{
    /// <summary>
    /// Validates that required permissions exists for at least one user role, which is assigned to at least one user.
    /// Throws an exception if;
    /// - no user role is found with a required permission,
    /// - all user roles with required permission are inactive,
    /// - no users are assigned to any of the found user roles with the required permission,
    /// - all users assigned to user role with required permission are inactive.
    /// </summary>
    /// <param name="excludedUsers">Specific users to exclude when validating required permissions.</param>
    Task ValidateExistsAsync(IEnumerable<UserId> excludedUsers);
}
