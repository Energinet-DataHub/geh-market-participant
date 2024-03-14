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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

public interface IMessageDelegationRepository
{
    Task<MessageDelegation?> GetAsync(MessageDelegationId id);
    Task<IEnumerable<MessageDelegation>> GetForActorAsync(ActorId delegatedBy);
    Task<IEnumerable<MessageDelegation>> GetDelegatedToActorAsync(ActorId delegatedTo);

    Task<MessageDelegation?> GetForActorAsync(ActorId delegatedBy, DelegationMessageType messageType);
    Task<MessageDelegationId> AddOrUpdateAsync(MessageDelegation messageDelegation);
}
