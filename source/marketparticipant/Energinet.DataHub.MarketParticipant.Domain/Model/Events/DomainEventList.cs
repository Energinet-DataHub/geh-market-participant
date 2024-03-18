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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class DomainEventList : IDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly Guid? _aggregateId;

    public DomainEventList()
    {
        _aggregateId = null;
    }

    public DomainEventList(Guid aggregateId)
    {
        _aggregateId = aggregateId;
    }

    public void Add(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    void IDomainEvents.ClearPublishedDomainEvents()
    {
        EnsureAggregateId();
        _domainEvents.Clear();
    }

    Guid IDomainEvents.GetAggregateIdForDomainEvents()
    {
        EnsureAggregateId();
        return _aggregateId.Value;
    }

    public IEnumerator<DomainEvent> GetEnumerator()
    {
        return _domainEvents.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _domainEvents.GetEnumerator();
    }

    [MemberNotNull(nameof(_aggregateId))]
    private void EnsureAggregateId()
    {
        if (_aggregateId == null)
            throw new InvalidOperationException("Cannot publish events for uncommitted aggregates.");
    }
}
