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
using Flurl.Http;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantContactClient : IMarketParticipantContactClient
    {
        private const string OrganizationsBaseUrl = "Organization";
        private const string ContactBaseUrl = "Contact";

        private readonly IFlurlClient _httpClient;

        public MarketParticipantContactClient(IFlurlClient client)
        {
            _httpClient = client;
        }

        public async Task<IEnumerable<ContactDto>> GetContactsAsync(Guid organizationId)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _httpClient.Request(OrganizationsBaseUrl, organizationId, ContactBaseUrl).GetAsync())
                .ConfigureAwait(false);

            var contacts = await response
                .GetJsonAsync<IEnumerable<ContactDto>>()
                .ConfigureAwait(false);

            return contacts;
        }

        public async Task<Guid> CreateContactAsync(Guid organizationId, CreateContactDto contactDto)
        {
            var response = await ValidationExceptionHandler
                .HandleAsync(
                    () => _httpClient.Request(OrganizationsBaseUrl, organizationId, ContactBaseUrl).PostJsonAsync(contactDto))
                .ConfigureAwait(false);

            var contact = await response
                .GetStringAsync()
                .ConfigureAwait(false);

            return Guid.Parse(contact);
        }

        public Task DeleteContactAsync(Guid organizationId, Guid contactId)
        {
            return ValidationExceptionHandler
                .HandleAsync(
                    () => _httpClient.Request(OrganizationsBaseUrl, organizationId, ContactBaseUrl, contactId).DeleteAsync());
        }
    }
}
