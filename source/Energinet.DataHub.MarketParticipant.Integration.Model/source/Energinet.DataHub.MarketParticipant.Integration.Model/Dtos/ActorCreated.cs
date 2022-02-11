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

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Dtos
{
    public sealed record ActorCreated
    {
        /// <summary>
        /// Specifies which data is available for consumption by a market operator.
        /// When a notification is received, the data is immediately made available for peeking.
        /// </summary>
        /// <param name="actorId">
        /// A guid uniquely identifying the data. This guid will be passed back
        /// to the sub-domain with the request for data to be generated.
        /// </param>
        public ActorCreated(Guid actorId)
        {
            ActorId = actorId;
        }

        public Guid ActorId { get; }
    }
}
