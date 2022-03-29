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

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public class Contact
    {
        public Contact(ContactCategory category, ContactName name, ContactEmail email, ContactPhone phone)
        {
            Id = new ContactId(Guid.Empty);
            Category = category;
            Name = name;
            Email = email;
            Phone = phone;
        }

        public Contact(ContactId id, ContactCategory category, ContactName name, ContactEmail email, ContactPhone phone)
        {
            Id = id;
            Category = category;
            Name = name;
            Email = email;
            Phone = phone;
        }

        public ContactId Id { get; set; }
        public ContactCategory Category { get; set; }
        public ContactName Name { get; set; }
        public ContactEmail Email { get; set; }
        public ContactPhone Phone { get; set; }
    }

    public sealed record ContactName(string Value);

    public sealed record ContactEmail(string Value);

    public sealed record ContactPhone(string Value);
}
