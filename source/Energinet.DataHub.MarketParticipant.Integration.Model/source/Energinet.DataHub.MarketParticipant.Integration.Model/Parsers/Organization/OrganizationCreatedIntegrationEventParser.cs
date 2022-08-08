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
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Organization
{
    public sealed class OrganizationCreatedIntegrationEventParser : IOrganizationCreatedIntegrationEventParser
    {
        public byte[] Parse(OrganizationCreatedIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = new OrganizationCreatedIntegrationEventContract()
                {
                    Id = integrationEvent.Id.ToString(),
                    EventCreated = Timestamp.FromDateTime(integrationEvent.EventCreated),
                    OrganizationId = integrationEvent.OrganizationId.ToString(),
                    Name = integrationEvent.Name,
                    BusinessRegisterIdentifier = integrationEvent.BusinessRegisterIdentifier,
                    Address = new OrganizationAddressEventData
                    {
                        City = integrationEvent.Address.City,
                        Country = integrationEvent.Address.Country,
                        Number = integrationEvent.Address.Number,
                        StreetName = integrationEvent.Address.StreetName,
                        ZipCode = integrationEvent.Address.ZipCode
                    },
                    Status = (int)integrationEvent.Status
                };

                if (integrationEvent.Comment != null)
                {
                    contract.Comment = integrationEvent.Comment;
                }

                return contract.ToByteArray();
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(OrganizationCreatedIntegrationEventContract)}", ex);
            }
        }

        internal OrganizationCreatedIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = OrganizationCreatedIntegrationEventContract.Parser.ParseFrom(protoContract);

                var createdEvent = new OrganizationCreatedIntegrationEvent(
                    Guid.Parse(contract.Id),
                    contract.EventCreated.ToDateTime(),
                    Guid.Parse(contract.OrganizationId),
                    contract.Name,
                    contract.BusinessRegisterIdentifier,
                    new Address(
                        contract.Address.StreetName,
                        contract.Address.Number,
                        contract.Address.ZipCode,
                        contract.Address.City,
                        contract.Address.Country),
                    Enum.IsDefined((OrganizationStatus)contract.Status) ? (OrganizationStatus)contract.Status : throw new FormatException(nameof(contract.Status)));

                if (contract.HasComment)
                {
                    createdEvent.Comment = contract.Comment;
                }

                return createdEvent;
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(OrganizationCreatedIntegrationEvent)}", ex);
            }
        }
    }
}
