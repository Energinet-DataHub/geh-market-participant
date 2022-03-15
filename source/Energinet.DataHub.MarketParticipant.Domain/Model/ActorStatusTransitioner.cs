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
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    internal sealed class ActorStatusTransitioner
    {
        public ActorStatusTransitioner()
        {
            Status = ActorStatus.New;
        }

        public ActorStatusTransitioner(ActorStatus status)
        {
            Status = status;
        }

        public ActorStatus Status { get; private set; }

        public void Activate()
        {
            EnsureCorrectState(ActorStatus.Active, ActorStatus.New, ActorStatus.Inactive, ActorStatus.Passive);
            Status = ActorStatus.Active;
        }

        public void Deactivate()
        {
            EnsureCorrectState(ActorStatus.Inactive, ActorStatus.Active, ActorStatus.Passive);
            Status = ActorStatus.Inactive;
        }

        public void SetAsPassive()
        {
            EnsureCorrectState(ActorStatus.Passive, ActorStatus.Active, ActorStatus.Inactive);
            Status = ActorStatus.Passive;
        }

        public void Delete()
        {
            EnsureCorrectState(ActorStatus.Deleted, ActorStatus.New, ActorStatus.Active, ActorStatus.Inactive, ActorStatus.Passive);
            Status = ActorStatus.Deleted;
        }

        private void EnsureCorrectState(ActorStatus targetState, params ActorStatus[] allowedStates)
        {
            if (!allowedStates.Contains(Status) && targetState != Status)
            {
                throw new InvalidOperationException($"Cannot change state from {Status} to {targetState}.");
            }
        }
    }
}
