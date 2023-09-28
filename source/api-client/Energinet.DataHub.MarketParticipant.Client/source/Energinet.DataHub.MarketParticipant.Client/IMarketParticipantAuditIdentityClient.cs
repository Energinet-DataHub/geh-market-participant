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
using Energinet.DataHub.MarketParticipant.Client.Models;

namespace Energinet.DataHub.MarketParticipant.Client
{
    /// <summary>
    /// Looks up information about an audit identity.
    /// </summary>
    public interface IMarketParticipantAuditIdentityClient
    {
        /// <summary>
        ///  Looks up information about an audit identity.
        /// </summary>
        /// <param name="auditIdentityId">The id of the audit identity.</param>
        /// <returns>The information about the audit identity.</returns>
        Task<GetAuditIdentityResponseDto> GetAsync(Guid auditIdentityId);
    }
}
