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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

/// <summary>
/// Service for user role audit logs
/// </summary>
public interface IUserRoleHelperService
{
    /// <summary>
    /// Creates audit log for user role created
    /// </summary>
    /// <param name="permissions">current user id</param>
    /// <param name="eicFunction">current user role id</param>
    /// <returns>User role audit log for saving</returns>
    Task<bool> EnsurePermissionsSelectedAreValidForMarketRoleAsync(
        IEnumerable<Permission> permissions,
        EicFunction eicFunction);
}
