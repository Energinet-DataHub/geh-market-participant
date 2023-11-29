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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    /// <summary>
    /// Service for accessing Azure AD.
    /// </summary>
    public interface IActiveDirectoryB2CService
    {
        /// <summary>
        /// Assigns an application registration to the given actor.
        /// </summary>
        /// <param name="actor">The actor for which to create an app and service principal.</param>
        Task AssignApplicationRegistrationAsync(Actor actor);

        /// <summary>
        /// Deletes the app and service prinicipal, for the given actor, from active directory.
        /// </summary>
        /// <param name="actor">The actor for which to remove the app and service principal.</param>
        Task DeleteAppRegistrationAsync(Actor actor);
    }
}
