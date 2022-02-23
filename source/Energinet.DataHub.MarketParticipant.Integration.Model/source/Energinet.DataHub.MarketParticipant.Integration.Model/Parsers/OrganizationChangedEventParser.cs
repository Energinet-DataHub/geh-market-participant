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

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers
{
    public sealed class OrganizationChangedEventParser : IOrganizationChangedEventParser
    {
        public OrganizationChangedEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = OrganizationChangedEventContract.Parser.ParseFrom(protoContract);

                return new OrganizationChangedEvent(
                    id: Guid.Parse(contract.Id),
                    actorId: !string.IsNullOrWhiteSpace(contract.ActorId) ? Guid.Parse(contract.ActorId) : null,
                    gln: contract.Gln,
                    name: contract.Name);
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(OrganizationChangedEvent)}", ex);
            }
        }

        public byte[] Parse(OrganizationChangedEvent changedEvent)
        {
            try
            {
                Guard.ThrowIfNull(changedEvent, nameof(changedEvent));

                return new OrganizationChangedEventContract
                {
                    Id = changedEvent.Id.ToString(),
                    ActorId = changedEvent.ActorId?.ToString(),
                    Gln = changedEvent.Gln,
                    Name = changedEvent.Name,
                }.ToByteArray();
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(OrganizationChangedEvent)}", ex);
            }
        }
    }
}
