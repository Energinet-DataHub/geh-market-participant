﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Mappers;

internal static class OrganizationMapper
{
    public static void MapToEntity(Organization from, OrganizationEntity to)
    {
        to.Id = from.Id.Value;
        to.Name = from.Name;
        to.BusinessRegisterIdentifier = from.BusinessRegisterIdentifier.Identifier;
        to.Status = (int)from.Status;

        var domainsToDelete = to.Domains.Where(d => !from.Domains.Any(d2 => d2.Value == d.Domain)).ToList();
        foreach (var domain in domainsToDelete)
        {
            to.Domains.Remove(domain);
        }

        foreach (var domain in from.Domains)
        {
            if (to.Domains.Any(d => d.Domain == domain.Value))
            {
                continue;
            }

            to.Domains.Add(new OrganizationDomainEntity
            {
                Domain = domain.Value
            });
        }

        MapAddressToEntity(from.Address, to);
    }

    public static Organization MapFromEntity(OrganizationEntity from)
    {
        return new Organization(
            new OrganizationId(from.Id),
            from.Name,
            new BusinessRegisterIdentifier(from.BusinessRegisterIdentifier),
            MapAddress(from),
            from.Domains.Select(d => new OrganizationDomain(d.Domain)),
            (OrganizationStatus)from.Status);
    }

    private static Address MapAddress(OrganizationEntity from)
    {
        return new Address(
            from.StreetName,
            from.Number,
            from.ZipCode,
            from.City,
            from.Country);
    }

    private static void MapAddressToEntity(Address from, OrganizationEntity to)
    {
        to.StreetName = from.StreetName;
        to.Number = from.Number;
        to.ZipCode = from.ZipCode;
        to.City = from.City;
        to.Country = from.Country;
    }
}
