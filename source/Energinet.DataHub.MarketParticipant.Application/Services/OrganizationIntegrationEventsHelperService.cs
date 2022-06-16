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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public class OrganizationIntegrationEventsHelperService : IOrganizationIntegrationEventsHelperService
    {
        public IEnumerable<IIntegrationEvent> BuildOrganizationCreatedEvents(Organization organization)
        {
            return new List<IIntegrationEvent>()
            {
                new OrganizationCreatedIntegrationEvent
                {
                    Address = organization.Address,
                    Name = organization.Name,
                    OrganizationId = organization.Id,
                    BusinessRegisterIdentifier = organization.BusinessRegisterIdentifier,
                    Comment = organization.Comment
                }
            };
        }

        public IEnumerable<IIntegrationEvent> DetermineOrganizationUpdatedChangeEvents(Organization organization, ChangeOrganizationDto organizationDto)
        {
            ArgumentNullException.ThrowIfNull(organization, nameof(organization));
            ArgumentNullException.ThrowIfNull(organizationDto, nameof(organizationDto));

            var changeEvents = new List<IIntegrationEvent>();

            if (organization.Name != organizationDto.Name)
            {
                changeEvents.Add(new OrganizationNameChangedIntegrationEvent()
                {
                    Name = organizationDto.Name,
                    OrganizationId = organization.Id
                });
            }

            if (organization.BusinessRegisterIdentifier.Identifier != organizationDto.BusinessRegisterIdentifier)
            {
                changeEvents.Add(new OrganizationBusinessRegisterIdentifierChangedIntegrationEvent()
                {
                    BusinessRegisterIdentifier = new BusinessRegisterIdentifier(organizationDto.BusinessRegisterIdentifier),
                    OrganizationId = organization.Id
                });
            }

            if (organization.Comment != organizationDto.Comment)
            {
                changeEvents.Add(new OrganizationCommentChangedIntegrationEvent()
                {
                    Comment = organizationDto.Comment,
                    OrganizationId = organization.Id
                });
            }

            if (organizationDto.Address.StreetName != organization.Address.StreetName ||
                organizationDto.Address.City != organization.Address.City ||
                organizationDto.Address.Country != organization.Address.Country ||
                organizationDto.Address.Number != organization.Address.Number ||
                organizationDto.Address.ZipCode != organization.Address.ZipCode)
            {
                changeEvents.Add(new OrganizationAddressChangedIntegrationEvent()
                {
                    Address = new Address(
                        organizationDto.Address.StreetName,
                        organizationDto.Address.Number,
                        organizationDto.Address.ZipCode,
                        organizationDto.Address.City,
                        organizationDto.Address.Country),
                    OrganizationId = organization.Id
                });
            }

            return changeEvents;
        }
    }
}
