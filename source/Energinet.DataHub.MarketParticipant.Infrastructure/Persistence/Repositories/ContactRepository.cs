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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly IMarketParticipantDbContext _marketParticipantDbContext;

        public ContactRepository(IMarketParticipantDbContext marketParticipantDbContext)
        {
            _marketParticipantDbContext = marketParticipantDbContext;
        }

        public async Task<ContactId> AddOrUpdateAsync(Contact contact)
        {
            Guard.ThrowIfNull(contact, nameof(contact));

            ContactEntity destination;

            if (contact.Id.Value == default)
            {
                destination = new ContactEntity();
            }
            else
            {
                destination = await _marketParticipantDbContext
                    .Contacts
                    .FindAsync(contact.Id.Value)
                    .ConfigureAwait(false);
            }

            ContactMapper.MapToEntity(contact, destination);
            _marketParticipantDbContext.Contacts.Update(destination);

            await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
            return new ContactId(destination.Id);
        }

        public async Task<Contact?> GetAsync(ContactId id)
        {
            Guard.ThrowIfNull(id, nameof(id));

            var contact = await _marketParticipantDbContext.Contacts
                .FindAsync(id.Value)
                .ConfigureAwait(false);

            return contact is null ? null : ContactMapper.MapFromEntity(contact);
        }
    }
}
