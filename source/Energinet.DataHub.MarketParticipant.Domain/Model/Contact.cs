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
    public sealed class Contact
    {
        public Contact(
            OrganizationId organizationId,
            string name,
            ContactCategory category,
            EmailAddress emailAddress,
            PhoneNumber? phone)
        {
            Id = new ContactId(Guid.Empty);
            OrganizationId = organizationId;
            Name = name;
            Category = category;
            EmailAddress = emailAddress;
            Phone = phone;
        }

        public Contact(
            ContactId id,
            OrganizationId organizationId,
            string name,
            ContactCategory category,
            EmailAddress emailAddress,
            PhoneNumber? phone)
        {
            Id = id;
            OrganizationId = organizationId;
            Name = name;
            Category = category;
            EmailAddress = emailAddress;
            Phone = phone;
        }

        public ContactId Id { get; }
        public OrganizationId OrganizationId { get; }

        public string Name { get; }
        public ContactCategory Category { get; }
        public EmailAddress EmailAddress { get; }
        public PhoneNumber? Phone { get; }
    }
}
