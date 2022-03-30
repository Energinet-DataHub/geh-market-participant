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

using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Application.Mappers
{
    public static class ContactMapper
    {
        public static ContactDto Map(Contact contact)
        {
            Guard.ThrowIfNull(contact, nameof(contact));
            return new ContactDto(
                contact.Id.Value,
                contact.Category.Name,
                contact.Name,
                contact.Email.Address,
                contact.Phone?.Number);
        }
    }
}
