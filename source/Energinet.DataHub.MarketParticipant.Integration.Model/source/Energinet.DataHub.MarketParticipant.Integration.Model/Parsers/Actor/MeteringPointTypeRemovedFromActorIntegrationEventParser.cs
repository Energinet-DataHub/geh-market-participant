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
    public sealed class MeteringPointTypeRemovedFromActorIntegrationEventParser : IMeteringPointTypeRemovedFromActorIntegrationEventParser
    {
        public byte[] Parse(MeteringPointTypeRemovedFromActorIntegrationEvent meteringPointTypeRemovedFromActorIntegrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(meteringPointTypeRemovedFromActorIntegrationEvent, nameof(meteringPointTypeRemovedFromActorIntegrationEvent));

                var contract = new MeteringPointTypeRemovedFromActorIntegrationEventContract
                {
                    Id = meteringPointTypeRemovedFromActorIntegrationEvent.Id.ToString(),
                    ActorId = meteringPointTypeRemovedFromActorIntegrationEvent.ActorId.ToString(),
                    OrganizationId = meteringPointTypeRemovedFromActorIntegrationEvent.OrganizationId.ToString(),
                    EventCreated = Timestamp.FromDateTime(meteringPointTypeRemovedFromActorIntegrationEvent.EventCreated),
                    MarketRoleFunction = (int)meteringPointTypeRemovedFromActorIntegrationEvent.Function,
                    GridAreaId = meteringPointTypeRemovedFromActorIntegrationEvent.GridAreaId.ToString(),
                    MeteringPointType = meteringPointTypeRemovedFromActorIntegrationEvent.MeteringPointType,
                    Type = meteringPointTypeRemovedFromActorIntegrationEvent.Type
                };

                return contract.ToByteArray();
            }
            catch (Exception e) when (e is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(MeteringPointTypeRemovedFromActorIntegrationEventContract)}", e);
            }
        }

        internal MeteringPointTypeRemovedFromActorIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = MeteringPointTypeRemovedFromActorIntegrationEventContract.Parser.ParseFrom(protoContract);

                var integrationEvent = new MeteringPointTypeRemovedFromActorIntegrationEvent(
                    Guid.Parse(contract.Id),
                    Guid.Parse(contract.ActorId),
                    Guid.Parse(contract.OrganizationId),
                    Enum.IsDefined(typeof(EicFunction), contract.MarketRoleFunction) ? (EicFunction)contract.MarketRoleFunction : throw new FormatException(nameof(contract.MarketRoleFunction)),
                    Guid.Parse(contract.GridAreaId),
                    contract.MeteringPointType,
                    contract.EventCreated.ToDateTime());

                if (integrationEvent.Type != contract.Type)
                {
                    throw new FormatException("Invalid Type");
                }

                return integrationEvent;
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(MeteringPointTypeRemovedFromActorIntegrationEvent)}", ex);
            }
        }
    }
}
