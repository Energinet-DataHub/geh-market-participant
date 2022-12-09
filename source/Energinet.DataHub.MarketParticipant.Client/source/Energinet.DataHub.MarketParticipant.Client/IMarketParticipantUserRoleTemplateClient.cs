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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;

namespace Energinet.DataHub.MarketParticipant.Client
{
    /// <summary>
    /// Manages user role templates.
    /// </summary>
    public interface IMarketParticipantUserRoleTemplateClient
    {
        /// <summary>
        /// Gets user role templates assigned to the specified user and actor.
        /// </summary>
        /// <param name="actorId">The id of the actor.</param>
        /// <param name="userId">The id of the user.</param>
        /// <returns>The list of currently assigned user role templates.</returns>
        Task<IEnumerable<UserRoleTemplateDto>> GetAsync(Guid actorId, Guid userId);

        /// <summary>
        /// Gets all user role templates that can be assigned to the specified actor.
        /// </summary>
        /// <param name="actorId">The id of the actor.</param>
        /// <returns>The list of assignable user role templates.</returns>
        Task<IEnumerable<UserRoleTemplateDto>> GetAssignableAsync(Guid actorId);
    }
}
