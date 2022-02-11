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
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers
{
    public sealed class ActorCreatedParser : IActorCreatedParser
    {
        public ActorCreated Parse(byte[] actorCreatedContract)
        {
            try
            {
                var contract = ActorCreatedContract.Parser.ParseFrom(actorCreatedContract);

                return new ActorCreated(actorId: Guid.Parse(contract.ActorId));
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(ActorCreated)}", ex);
            }
        }
    }
}
