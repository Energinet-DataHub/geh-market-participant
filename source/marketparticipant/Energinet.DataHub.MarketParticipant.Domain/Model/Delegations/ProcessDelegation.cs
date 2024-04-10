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
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;

public sealed class ProcessDelegation : IPublishDomainEvents
{
    private readonly DomainEventList _domainEvents;
    private readonly List<DelegationPeriod> _delegations = [];
    private readonly List<GridAreaId> _allowedGridAreas = [];

    public ProcessDelegation(Actor processOwner, DelegatedProcess process)
    {
        ArgumentNullException.ThrowIfNull(processOwner);

        if (processOwner.MarketRoles.All(role =>
                role.Function != EicFunction.GridAccessProvider
                && role.Function != EicFunction.BalanceResponsibleParty
                && role.Function != EicFunction.EnergySupplier))
        {
            throw new ValidationException("Actor's market role does not support process delegation.")
                .WithErrorCode("process_delegation.actor_invalid_market_role");
        }

        DelegatedBy = processOwner.Id;
        Process = process;

        _allowedGridAreas.AddRange(processOwner
            .MarketRoles
            .SelectMany(mr => mr.GridAreas)
            .Select(ga => ga.Id));

        _domainEvents = new DomainEventList();
    }

    public ProcessDelegation(
        ProcessDelegationId id,
        ActorId delegatedBy,
        IEnumerable<GridAreaId> actorGridAreas,
        DelegatedProcess process,
        Guid concurrencyToken,
        IEnumerable<DelegationPeriod> delegations)
    {
        Id = id;
        DelegatedBy = delegatedBy;
        Process = process;
        ConcurrencyToken = concurrencyToken;
        _domainEvents = new DomainEventList(Id.Value);
        _delegations.AddRange(delegations);
        _allowedGridAreas.AddRange(actorGridAreas);
    }

    public ProcessDelegationId Id { get; } = new(Guid.Empty);
    public ActorId DelegatedBy { get; }
    public DelegatedProcess Process { get; }
    public Guid ConcurrencyToken { get; }

    public IReadOnlyCollection<DelegationPeriod> Delegations => _delegations;

    IDomainEvents IPublishDomainEvents.DomainEvents => _domainEvents;

    public void DelegateTo(ActorId delegatedTo, GridAreaId gridAreaId, Instant startsAt)
    {
        var delegationPeriod = new DelegationPeriod(delegatedTo, gridAreaId, startsAt);

        if (IsThereDelegationPeriodOverlap(startsAt, gridAreaId))
        {
            throw new ValidationException("Delegation already exists for the given grid area and time period")
                .WithErrorCode("process_delegation.overlap");
        }

        if (_allowedGridAreas.Count > 0 && !_allowedGridAreas.Contains(gridAreaId))
        {
            throw new ValidationException("Actor cannot delegate for a grid area it is not responsible for.")
                .WithErrorCode("process_delegation.grid_area_not_allowed");
        }

        _delegations.Add(delegationPeriod);
        _domainEvents.Add(new ProcessDelegationConfigured(this, delegationPeriod));
    }

    public void StopDelegation(DelegationPeriod existingPeriod, Instant? stopsAt)
    {
        ArgumentNullException.ThrowIfNull(existingPeriod);

        if (!_delegations.Remove(existingPeriod))
        {
            throw new InvalidOperationException("Provided existing delegation period was not in collection.");
        }

        if (existingPeriod.IsCancelled)
        {
            throw new ValidationException("Cannot stop a cancelled delegation.")
                .WithErrorCode("process_delegation.cancelled");
        }

        if (IsThereDelegationPeriodOverlap(existingPeriod.StartsAt, existingPeriod.GridAreaId, stopsAt))
        {
            throw new ValidationException("Delegation already exists for the given grid area and time period.")
                .WithErrorCode("process_delegation.overlap");
        }

        var delegationPeriod = existingPeriod with { StopsAt = stopsAt };
        _delegations.Add(delegationPeriod);
        _domainEvents.Add(new ProcessDelegationConfigured(this, delegationPeriod));
    }

    private bool IsThereDelegationPeriodOverlap(Instant startsAt, GridAreaId gridAreaId, Instant? stopsAt = null)
    {
        return _delegations
             .Where(x => x.GridAreaId == gridAreaId && !(x.StopsAt <= x.StartsAt))
             .Any(x => x.StartsAt < (stopsAt ?? Instant.MaxValue) && (x.StopsAt ?? Instant.MaxValue) > startsAt);
    }
}
