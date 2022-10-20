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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Application.Mappers
{
    public static class OrganizationMapper
    {
        public static OrganizationDto Map(Organization organization)
        {
            ArgumentNullException.ThrowIfNull(organization, nameof(organization));
            return new OrganizationDto(
                organization.Id.Value,
                organization.Name,
                organization.BusinessRegisterIdentifier.Identifier,
                organization.Comment,
                organization.Status,
                Map(organization.Address),
                organization.Actors.Select(Map).ToList());
        }

        public static ActorDto Map(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));
            return new ActorDto(
                actor.Id,
                actor.ExternalActorId?.Value,
                new ActorNumberDto(actor.ActorNumber.Value),
                actor.Status,
                new ActorNameDto(actor.Name.Value),
                actor.MarketRoles.Select(Map).ToList());
        }

        private static AddressDto Map(Address address)
        {
            return new AddressDto(
                address.StreetName,
                address.Number,
                address.ZipCode,
                address.City,
                address.Country);
        }

        private static ActorMarketRoleDto Map(ActorMarketRole marketRole)
        {
            return new ActorMarketRoleDto(
                marketRole.Function,
                marketRole.GridAreas.Select(e => new ActorGridAreaDto(e.Id, e.MeteringPointTypes)));
        }
    }
}
