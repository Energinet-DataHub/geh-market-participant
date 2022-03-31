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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers
{
    internal static class OrganizationMapper
    {
        public static void MapToEntity(Organization from, OrganizationEntity to)
        {
            to.Id = from.Id.Value;
            to.Name = from.Name;
            to.CVR = from.CVR.Number;
            MapAddressToEntity(from.Address, to.Address);
            var actorEntities = to.Actors.ToDictionary(x => x.Id);

            foreach (var actor in from.Actors)
            {
                if (actorEntities.TryGetValue(actor.Id, out var existing))
                {
                    MapActorEntity(actor, existing);
                }
                else
                {
                    var newActor = new ActorEntity();
                    MapActorEntity(actor, newActor);
                    to.Actors.Add(newActor);
                }
            }
        }

        public static Organization MapFromEntity(OrganizationEntity from)
        {
            return new Organization(
                new OrganizationId(from.Id),
                from.Name,
                MapEntitiesToActors(from.Actors),
                new CVRNumber(from.CVR),
                MapAddress(from.Address));
        }

        private static Address MapAddress(AddressEntity from)
        {
            return new Address(
                from.StreetName,
                from.Number,
                from.ZipCode,
                from.City,
                from.Country);
        }

        private static void MapAddressToEntity(Address from, AddressEntity to)
        {
            to.StreetName = from.StreetName;
            to.Number = from.Number;
            to.ZipCode = from.ZipCode;
            to.City = from.City;
            to.Country = from.Country;
        }

        private static void MapActorEntity(Actor from, ActorEntity to)
        {
            to.Id = from.Id;
            to.ActorId = from.ExternalActorId.Value;
            to.Gln = from.Gln.Value;
            to.Status = (int)from.Status;

            if (from.Areas.Any())
            {
                var gridArea = to.SingleGridArea ?? new GridAreaEntity();
                GridAreaMapper.MapToEntity(from.Areas.Single(), gridArea);
                to.SingleGridArea = gridArea;
            }

            // MeteringPointTypes are currently treated as value types, so they are deleted and recreated with each update.
            to.MeteringPointTypes.Clear();
            foreach (var meteringPointType in from.MeteringPointTypes)
            {
                to.MeteringPointTypes.Add(meteringPointType);
            }

            // Market roles are currently treated as value types, so they are deleted and recreated with each update.
            to.MarketRoles.Clear();
            foreach (var marketRole in from.MarketRoles)
            {
                to.MarketRoles.Add(new MarketRoleEntity { Function = (int)marketRole.Function });
            }
        }

        private static IEnumerable<Actor> MapEntitiesToActors(IEnumerable<ActorEntity> actors)
        {
            return actors.Select(actor =>
            {
                var marketRoles = actor.MarketRoles.Select(marketRole =>
                {
                    var function = (EicFunction)marketRole.Function;
                    return new MarketRole(function);
                });

                var actorGln = new GlobalLocationNumber(actor.Gln);
                var actorStatus = (ActorStatus)actor.Status;
                var gridArea = actor.SingleGridArea != null
                    ? new[] { GridAreaMapper.MapFromEntity(actor.SingleGridArea) }
                    : Array.Empty<GridArea>();

                return new Actor(
                    actor.Id,
                    new ExternalActorId(actor.ActorId),
                    actorGln,
                    actorStatus,
                    gridArea,
                    marketRoles,
                    actor.MeteringPointTypes);
            }).ToList();
        }
    }
}
