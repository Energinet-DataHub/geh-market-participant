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
using System.Collections.ObjectModel;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class Actor
    {
        private readonly ActorStatusTransitioner _actorStatusTransitioner;

        public Actor(ActorNumber actorNumber)
        {
            Id = Guid.NewGuid();
            ExternalActorId = null;
            ActorNumber = actorNumber;
            _actorStatusTransitioner = new ActorStatusTransitioner();
            MarketRoles = new Collection<ActorMarketRole>();
        }

        public Actor(
            Guid id,
            ExternalActorId? externalActorId,
            ActorNumber actorNumber,
            ActorStatus actorStatus,
            IEnumerable<ActorMarketRole> marketRoles)
        {
            Id = id;
            ExternalActorId = externalActorId;
            ActorNumber = actorNumber;
            _actorStatusTransitioner = new ActorStatusTransitioner(actorStatus);
            MarketRoles = new List<ActorMarketRole>(marketRoles);
        }

        /// <summary>
        /// The internal id of actor.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The external actor id for integrating Azure AD and domains.
        /// </summary>
        public ExternalActorId? ExternalActorId { get; set; }

        /// <summary>
        /// The global location number of the current actor.
        /// </summary>
        public ActorNumber ActorNumber { get; }

        /// <summary>
        /// The status of the current actor.
        /// </summary>
        public ActorStatus Status
        {
            get => _actorStatusTransitioner.Status;
            set => _actorStatusTransitioner.Status = value;
        }

        /// <summary>
        /// The roles (functions and permissions) assigned to the current actor.
        /// </summary>
        public ICollection<ActorMarketRole> MarketRoles { get; }

        /// <summary>
        /// Activates the current actor, the status changes to Active.
        /// Only New, Inactive and Passive actors can be activated.
        /// </summary>
        public void Activate() => _actorStatusTransitioner.Activate();

        /// <summary>
        /// Deactivates the current actor, the status changes to Inactive.
        /// Only Active and Passive actors can be deactivated.
        /// </summary>
        public void Deactivate() => _actorStatusTransitioner.Deactivate();

        /// <summary>
        /// Passive actors have certain domain-specific actions that can be performed.
        /// Only Active and Inactive actors can be set to passive.
        /// </summary>
        public void SetAsPassive() => _actorStatusTransitioner.SetAsPassive();

        /// <summary>
        /// Soft-deletes the current role, the status changes to Deleted.
        /// The role becomes read-only after deletion.
        /// </summary>
        public void Delete() => _actorStatusTransitioner.Delete();
    }
}
