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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Provides access to user roles.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Returns all existing Permissions
    /// </summary>
    /// <returns>All permissions with their details</returns>
    Task<IEnumerable<PermissionDetails>> GetAllAsync();

    /// <summary>
    /// Gets all permission that are available for a given <see cref="EicFunction"/> EicFunction.
    /// </summary>
    /// <param name="eicFunction">The eicFunction you want to get permissions for.</param>
    /// <returns>List of permissions for this EicFunction.</returns>
    Task<IEnumerable<PermissionDetails>> GetToMarketRoleAsync(EicFunction eicFunction);
}
