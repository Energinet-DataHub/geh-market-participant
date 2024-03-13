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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

public sealed class MessageDelegation
{
    private readonly List<DelegationPeriod> _delegations = [];

    public MessageDelegation(Actor messageOwner, DelegationMessageType messageType)
    {
        ArgumentNullException.ThrowIfNull(messageOwner);

        if (messageOwner.Status != ActorStatus.Active)
        {
            throw new ValidationException("Actor must be active to delegate messages.")
                .WithErrorCode("message_delegation.actor_inactive");
        }


        if (messageOwner.MarketRoles.All(role =>
                role.Function != EicFunction.GridAccessProvider
                && role.Function != EicFunction.BalanceResponsibleParty
                && role.Function != EicFunction.EnergySupplier
                && role.Function != EicFunction.BillingAgent))
        {
            throw new ValidationException("Actor must have a valid market role to delegate messages.")
                .WithErrorCode("message_delegation.actor_invalid_market_role");
        }


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

        // Rule There can't be any overlap between delegation period on a given grid area
        // but don't include periods where ExpiresAt <= StartsAt, because that means that it was cancelled.
        if (IsThereDelegationPeriodOverlap(startsAt, gridAreaId, stopsAt))
        {
            throw new ValidationException("Delegation already exists for the given grid area and time period")
                .WithErrorCode("message_delegation.overlap");
        }

        // TODO: Rule Denne regel gælder ikke, hvis Actor A/B er deaktiveret.
        _delegations.Add(delegationPeriod);
    }

    public void StopDelegation(DelegationPeriod existingPeriod, Instant stopsAt)
    {
        ArgumentNullException.ThrowIfNull(existingPeriod);

        if (!_delegations.Remove(existingPeriod))
        {
            throw new ValidationException("Provided existing delegation period was not in collection.")
                .WithErrorCode("message_delegation.does_not_exist");
        }

        // Rule There can't be any overlap between delegation period on a given grid area
        // but don't include periods where ExpiresAt <= StartsAt, because that means that it was cancelled.
        if (IsThereDelegationPeriodOverlap(existingPeriod.StartsAt, existingPeriod.GridAreaId, stopsAt))
        {
            throw new ValidationException("Delegation already exists for the given grid area and time period")
                .WithErrorCode("message_delegation.overlap");
        }

        // TODO: Rule Denne regel gælder ikke, hvis Actor A/B er deaktiveret.
        _delegations.Add(existingPeriod with { StopsAt = stopsAt });
    }

    private bool IsThereDelegationPeriodOverlap(Instant startsAt, GridAreaId gridAreaId, Instant? stopsAt = null)
    {
       return _delegations
            .Where(x => x.GridAreaId == gridAreaId && !(x.StopsAt <= x.StartsAt))
            .Any(x =>
            {
                var interval = new Interval(x.StartsAt, x.StopsAt);
                var intervalNew = new Interval(startsAt, stopsAt);
                return interval.Contains(startsAt) ||
                       (stopsAt.HasValue && interval.Contains(stopsAt.GetValueOrDefault())) ||
                       intervalNew.Contains(x.StartsAt) ||
                       (x.StopsAt.HasValue && intervalNew.Contains(x.StopsAt.GetValueOrDefault()));
            });
    }
}
