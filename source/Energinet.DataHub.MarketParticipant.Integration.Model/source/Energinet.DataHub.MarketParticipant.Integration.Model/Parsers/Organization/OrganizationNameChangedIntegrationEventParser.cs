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

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Organization
{
    public class OrganizationNameChangedIntegrationEventParser : IOrganizationNameChangedIntegrationEventParser
    {
        public byte[] Parse(OrganizationNameChangedIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = new OrganizationNameChangedIntegrationEventContract()
                {
                    Id = integrationEvent.Id.ToString(),
                    EventCreated = Timestamp.FromDateTime(integrationEvent.EventCreated),
                    OrganizationId = integrationEvent.OrganizationId.ToString(),
                    Name = integrationEvent.Name,
                    Type = integrationEvent.Type
                };

                return contract.ToByteArray();
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(OrganizationNameChangedIntegrationEvent)}", ex);
            }
        }

        internal OrganizationNameChangedIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = OrganizationNameChangedIntegrationEventContract.Parser.ParseFrom(protoContract);

                var integrationEvent = new OrganizationNameChangedIntegrationEvent(
                    Guid.Parse(contract.Id),
                    contract.EventCreated.ToDateTime(),
                    Guid.Parse(contract.OrganizationId),
                    contract.Name);

                if (integrationEvent.Type != contract.Type)
                {
                    throw new FormatException("Invalid Type");
                }

                return integrationEvent;
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(OrganizationNameChangedIntegrationEvent)}", ex);
            }
        }
    }
}
