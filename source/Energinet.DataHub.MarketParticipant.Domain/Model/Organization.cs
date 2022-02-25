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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class Organization
    {
        private readonly ICollection<IOrganizationRole> _roles;

        public Organization(
            Guid actorId,
            GlobalLocationNumber gln,
            string name)
        {
            Id = new OrganizationId(Guid.Empty);
            ActorId = actorId;
            Gln = gln;
            Name = name;
            _roles = new Collection<IOrganizationRole>();
        }

        public Organization(
            OrganizationId id,
            Guid actorId,
            GlobalLocationNumber gln,
            string name,
            IEnumerable<IOrganizationRole> roles)
        {
            Id = id;
            ActorId = actorId;
            Gln = gln;
            Name = name;
            _roles = roles.ToList();
        }

        public OrganizationId Id { get; }
        public Guid ActorId { get; }

        public GlobalLocationNumber Gln { get; }
        public string Name { get; }

        public IEnumerable<IOrganizationRole> Roles => _roles;

        public void AddRole(IOrganizationRole organizationRole)
        {
            Guard.ThrowIfNull(organizationRole, nameof(organizationRole));

            // TODO: Validation rules. Check for conflicting roles.
            _roles.Add(organizationRole);
        }
    }
}
