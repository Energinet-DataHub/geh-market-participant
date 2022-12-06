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
/// Provides access to user role templates.
/// </summary>
public interface IUserRoleTemplateRepository
{
    /// <summary>
    /// Gets the user role template having the specified external id.
    /// </summary>
    /// <param name="userRoleTemplateId">The id of the user role template.</param>
    /// <returns>The template if it exists; otherwise null.</returns>
    Task<UserRoleTemplate?> GetAsync(UserRoleTemplateId userRoleTemplateId);

    /// <summary>
    /// Gets user role templates that support the specified Eic-functions.
    /// </summary>
    /// <param name="eicFunctions">The list of Eic-functions the templates must support.</param>
    /// <returns>A list of templates.</returns>
    Task<IEnumerable<UserRoleTemplate>> GetAsync(IEnumerable<EicFunction> eicFunctions);
}
