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

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public abstract class ActorCredentials
    {
        protected ActorCredentials(
            ActorId actorId)
        {
            Id = new CredentialsId(0);
            ActorId = actorId;
        }

        protected ActorCredentials(
            CredentialsId id,
            ActorId actorId)
        {
            Id = id;
            ActorId = actorId;
        }

        public CredentialsId Id { get; }
        public ActorId ActorId { get; }
    }
}
