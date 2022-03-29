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

using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers
{
    internal sealed class ContactMapper
    {
        public static void MapToEntity(Contact from, ContactEntity to)
        {
            to.Category = from.Category;
            to.Email = from.Email.Value;
            to.Id = from.Id.Value;
            to.Name = from.Name.Value;
            to.Phone = from.Phone.Value;
        }

        public static Contact MapFromEntity(ContactEntity from)
        {
            return new Contact(
                new ContactId(from.Id),
                from.Category,
                new ContactName(from.Name),
                new ContactEmail(from.Email),
                new ContactPhone(from.Phone));
        }
    }
}
