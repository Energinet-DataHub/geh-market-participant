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
    /// BFF client for contacts in Energinet.DataHub.MarketParticipant.
    /// </summary>
    public interface IMarketParticipantContactClient
    {
        /// <summary>
        /// List all contacts for an organization.
        /// </summary>
        /// <param name="organizationId">The id of the organization.</param>
        /// <returns>A list of contacts <see cref="ActorDto"/> belonging to the organization.</returns>
        Task<IEnumerable<ContactDto>> GetContactsAsync(Guid organizationId);

        /// <summary>
        /// Creates a new contact in an organization.
        /// </summary>
        /// <param name="organizationId">The id of the organization.</param>
        /// <param name="contactDto">The details of the contact to create.</param>
        /// <returns>The id of the created contact.</returns>
        Task<Guid> CreateContactAsync(Guid organizationId, CreateContactDto contactDto);

        /// <summary>
        /// Removes the specified contact from an organization.
        /// </summary>
        /// <param name="organizationId">The id of the organization.</param>
        /// <param name="contactId">The id of the contact.</param>
        Task DeleteContactAsync(Guid organizationId, Guid contactId);
    }
}
