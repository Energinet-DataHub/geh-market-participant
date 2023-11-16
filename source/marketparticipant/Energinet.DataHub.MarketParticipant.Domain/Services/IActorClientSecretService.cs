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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    /// <summary>
    /// Service for managing client secrets for actors.
    /// </summary>
    public interface IActorClientSecretService
    {
        /// <summary>
        /// Creates a client secret for an actor.
        /// </summary>
        /// <param name="actor">The actor for which to create a client secret for.</param>
        /// <returns>The secret created for the application</returns>
        /// <remarks>The secret returned can only be used while in memory, it is not available in clear text after this</remarks>
        Task<(Guid SecretId, string SecretText, Instant ExpirationDate)> CreateSecretAsync(Actor actor);

        /// <summary>
        /// Remove a given actors client secret.
        /// </summary>
        /// <param name="actor">The actor for which to remove secrets from.</param>
        Task RemoveSecretAsync(Actor actor);
    }
}
