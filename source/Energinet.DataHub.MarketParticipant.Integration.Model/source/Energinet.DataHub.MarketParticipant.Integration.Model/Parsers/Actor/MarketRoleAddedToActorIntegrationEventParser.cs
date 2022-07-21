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

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor
{
    public sealed class MarketRoleAddedToActorIntegrationEventParser : IMarketRoleAddedToActorIntegrationEventParser
    {
        public byte[] Parse(MarketRoleAddedToActorIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = new MarketRoleAddedToActorIntegrationEventContract
                {
                    Id = integrationEvent.Id.ToString(),
                    EventCreated = Timestamp.FromDateTime(integrationEvent.EventCreated),
                    ActorId = integrationEvent.ActorId.ToString(),
                    OrganizationId = integrationEvent.OrganizationId.ToString(),
                    BusinessRole = (int)integrationEvent.BusinessRoleCode,
                    MarketRoleFunction = (int)integrationEvent.MarketRole,
                    Type = integrationEvent.Type
                };

                return contract.ToByteArray();
            }
            catch (Exception e) when (e is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(MarketRoleAddedToActorIntegrationEventContract)}", e);
            }
        }

        internal MarketRoleAddedToActorIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = MarketRoleAddedToActorIntegrationEventContract.Parser.ParseFrom(protoContract);

                var integrationEvent = new MarketRoleAddedToActorIntegrationEvent(
                    Guid.Parse(contract.Id),
                    Guid.Parse(contract.ActorId),
                    Guid.Parse(contract.OrganizationId),
                    Enum.IsDefined(typeof(BusinessRoleCode), contract.BusinessRole) ? (BusinessRoleCode)contract.BusinessRole : throw new FormatException(nameof(contract.BusinessRole)),
                    Enum.IsDefined(typeof(EicFunction), contract.MarketRoleFunction) ? (EicFunction)contract.MarketRoleFunction : throw new FormatException(nameof(contract.MarketRoleFunction)),
                    contract.EventCreated.ToDateTime());

                if (integrationEvent.Type != contract.Type)
                {
                    throw new FormatException("Invalid Type");
                }

                return integrationEvent;
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(MarketRoleAddedToActorIntegrationEvent)}", ex);
            }
        }
    }
}
