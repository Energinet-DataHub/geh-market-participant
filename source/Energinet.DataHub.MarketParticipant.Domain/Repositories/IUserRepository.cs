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

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories
{
    /// <summary>
    /// Provides access to Users.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// List all permissions a user has through a given Actor
        /// </summary>
        /// <param name="externalActorId">The external id for the Actor you want the permissions for</param>
        /// <param name="externalUserId">The user you want the permissions for</param>
        /// <returns>A List of the applicable permissions</returns>
        Task<IEnumerable<Core.App.Common.Security.Permission>> GetPermissionsAsync(
            Guid externalActorId,
            Guid externalUserId);

        /// <summary>
        /// List all permissions a user has through a given Actor
        /// </summary>
        /// <param name="externalActorId">The external id for the Actor you want the permissions for</param>
        /// <param name="externalUserId">The user you want the permissions for</param>
        /// <returns>A List of the applicable permissions</returns>
        Task<IEnumerable<Core.App.Common.Security.Permission>> GetPermissionsWithJoinsAsync(
            Guid externalActorId,
            Guid externalUserId);
    }
}
