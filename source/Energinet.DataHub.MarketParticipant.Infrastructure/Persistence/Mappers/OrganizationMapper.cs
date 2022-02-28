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
using Energinet.DataHub.MarketParticipant.Domain.Model.Roles;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers
{
    internal static class OrganizationMapper
    {
        public static void MapToEntity(Organization from, OrganizationEntity to)
        {
            to.Id = from.Id.Value;
            to.ActorId = from.ActorId;
            to.Gln = from.Gln.Value;
            to.Name = from.Name;

            var roleEntities = to.Roles.ToDictionary(x => x.Id);
            foreach (var role in from.Roles)
            {
                if (roleEntities.TryGetValue(role.Id, out var existing))
                {
                    MapRoleEntity(role, existing);
                }
                else
                {
                    var newRole = new OrganizationRoleEntity();
                    MapRoleEntity(role, newRole);
                    to.Roles.Add(newRole);
                }
            }
        }

        public static Organization MapFromEntity(OrganizationEntity from)
        {
            return new Organization(
                new OrganizationId(from.Id),
                from.ActorId,
                new GlobalLocationNumber(from.Gln),
                from.Name,
                MapEntitiesToRoles(from.Roles));
        }

        private static void MapRoleEntity(IOrganizationRole from, OrganizationRoleEntity to)
        {
            to.Id = from.Id;
            to.Status = (int)from.Status;
            to.BusinessRole = (int)from.Code;

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

        private static IEnumerable<IOrganizationRole> MapEntitiesToRoles(IEnumerable<OrganizationRoleEntity> roles)
        {
            return roles.Select(role =>
            {
                var marketRoles = role.MarketRoles.Select(marketRole =>
                {
                    var function = (EicFunction)marketRole.Function;
                    return new MarketRole(function);
                });

                var businessRole = (BusinessRoleCode)role.BusinessRole;
                var roleStatus = (RoleStatus)role.Status;

                return (IOrganizationRole)(businessRole switch
                {
                    BusinessRoleCode.Ddk => new BalanceResponsiblePartyRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Ddm => new GridAccessProviderRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Ddq => new BalancePowerSupplierRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Ddx => new ImbalanceSettlementResponsibleRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Ddz => new MeteringPointAdministratorRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Dea => new MeteredDataAggregatorRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Ez => new SystemOperatorRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Mdr => new MeteredDataResponsibleRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    BusinessRoleCode.Sts => new DanishEnergyAgencyRole(
                        role.Id,
                        roleStatus,
                        marketRoles,
                        role.MeteringPointTypes),
                    _ => throw new ArgumentOutOfRangeException(nameof(role))
                });
            }).ToList();
        }
    }
}
