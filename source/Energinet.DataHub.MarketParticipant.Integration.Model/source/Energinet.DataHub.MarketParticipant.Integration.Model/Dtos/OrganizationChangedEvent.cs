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
    public sealed record OrganizationChangedEvent
    {
        /// <summary>
        /// An event representing af change to a given Organization.
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <param name="actorId">Actor ID</param>
        /// <param name="gln">GLN number</param>
        /// <param name="name">Name</param>
        public OrganizationChangedEvent(Guid id, Guid? actorId, string gln, string name)
        {
            Id = id;
            ActorId = actorId;
            Gln = gln;
            Name = name;
        }

        public Guid Id { get; }
        public Guid? ActorId { get; }
        public string Gln { get; }
        public string Name { get; }
    }
}
