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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

public class ActorDelegation
{
    public ActorDelegation(
        ActorDelegationId id,
        ActorId delegatedBy,
        ActorId delegatedTo,
        IReadOnlyCollection<GridAreaCode> gridAreas,
        DelegationMessageType messageType,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
    {
        Id = id;
        DelegatedBy = delegatedBy;
        DelegatedTo = delegatedTo;
        GridAreas = gridAreas;
        MessageType = messageType;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public ActorDelegationId Id { get; }
    public ActorId DelegatedBy { get; }
    public ActorId DelegatedTo { get; }
    public IReadOnlyCollection<GridAreaCode> GridAreas { get; }
    public DelegationMessageType MessageType { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ExpiresAt { get; internal set; }

    public void SetExpiresAt(DateTimeOffset expiresAt)
    {
        ExpiresAt = expiresAt;
    }
}
