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
using System.Linq;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

public sealed class MessageDelegation
{
    private readonly List<DelegationTarget> _targets = [];

    public MessageDelegation(Actor messageOwner, DelegationMessageType messageType)
    {
        ArgumentNullException.ThrowIfNull(messageOwner);

        if (messageOwner.Status != ActorStatus.Active)
            throw new InvalidOperationException("Actor must be active to delegate messages.");

        if (messageOwner.MarketRoles.All(role =>
                role.Function != EicFunction.GridAccessProvider
                && role.Function != EicFunction.BalanceResponsibleParty
                && role.Function != EicFunction.EnergySupplier
                && role.Function != EicFunction.BillingAgent))
            throw new InvalidOperationException("Actor must have a valid market role to delegate messages.");

        DelegatedBy = messageOwner.Id;
        MessageType = messageType;
    }

    public MessageDelegation(
        MessageDelegationId id,
        ActorId delegatedBy,
        DelegationMessageType messageType,
        IEnumerable<DelegationTarget> destinations)
    {
        Id = id;
        DelegatedBy = delegatedBy;
        MessageType = messageType;
        _targets.AddRange(destinations);
    }

    public MessageDelegationId Id { get; } = new(Guid.Empty);
    public ActorId DelegatedBy { get; }
    public DelegationMessageType MessageType { get; }
    public IReadOnlyCollection<DelegationTarget> Targets => _targets;

    public void DelegateTo(Actor target, GridAreaId gridAreaId, Instant from)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(gridAreaId);

        var delegationPeriod = new DelegationTarget(target.Id, gridAreaId, from);

        // TODO: Rule (A/Fra, MessageType) skal være unik i perioden i netområde.
        // TODO: Rule Denne regel gælder ikke, hvis ExpiresAt <= StartsAt.
        // TODO: Rule Denne regel gælder ikke, hvis Actor A/B er deaktiveret.
        _targets.Add(delegationPeriod);
    }

    public void StopDelegation(DelegationTargetId delegationTargetId, Instant stopsAt)
    {
        var delegationPeriod = _targets.Single(p => p.Id == delegationTargetId);

        // TODO: Rule (A/Fra, MessageType) skal være unik i perioden i netområde.
        // TODO: Rule Denne regel gælder ikke, hvis ExpiresAt <= StartsAt.
        // TODO: Rule Denne regel gælder ikke, hvis Actor A/B er deaktiveret.
        delegationPeriod.StopsAt = stopsAt;
    }
}
