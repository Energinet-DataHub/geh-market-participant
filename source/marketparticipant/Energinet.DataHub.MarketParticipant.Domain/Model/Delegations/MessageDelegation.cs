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
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

public sealed class MessageDelegation
{
    private readonly List<DelegationPeriod> _delegations = [];

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
        Guid concurrencyToken,
        IEnumerable<DelegationPeriod> delegations)
    {
        Id = id;
        DelegatedBy = delegatedBy;
        MessageType = messageType;
        ConcurrencyToken = concurrencyToken;
        _delegations.AddRange(delegations);
    }

    public MessageDelegationId Id { get; } = new(Guid.Empty);
    public ActorId DelegatedBy { get; }
    public DelegationMessageType MessageType { get; }
    public Guid ConcurrencyToken { get; }

    public IReadOnlyCollection<DelegationPeriod> Delegations => _delegations;

    public void DelegateTo(ActorId delegatedTo, GridAreaId gridAreaId, Instant startsAt, Instant? stopsAt = null)
    {
        var delegationPeriod = new DelegationPeriod(delegatedTo, gridAreaId, startsAt, stopsAt);

        // TODO: Rule (A/Fra, MessageType) skal være unik i perioden i netområde.
        // TODO: Rule Denne regel gælder ikke, hvis ExpiresAt <= StartsAt.
        // TODO: Rule Denne regel gælder ikke, hvis Actor A/B er deaktiveret.
        _delegations.Add(delegationPeriod);
    }

    public void StopDelegation(DelegationPeriod existingPeriod, Instant stopsAt)
    {
        ArgumentNullException.ThrowIfNull(existingPeriod);

        if (!_delegations.Remove(existingPeriod))
        {
            throw new InvalidOperationException("Provided existing delegation period was not in collection.");
        }

        // TODO: Rule (A/Fra, MessageType) skal være unik i perioden i netområde.
        // TODO: Rule Denne regel gælder ikke, hvis ExpiresAt <= StartsAt.
        // TODO: Rule Denne regel gælder ikke, hvis Actor A/B er deaktiveret.
        _delegations.Add(existingPeriod with { StopsAt = stopsAt });
    }
}
