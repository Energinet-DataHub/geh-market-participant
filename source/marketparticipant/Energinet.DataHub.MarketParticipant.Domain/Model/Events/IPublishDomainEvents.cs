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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

/// <summary>
/// Specifies that the aggregate publishes domain events.
/// </summary>
public interface IPublishDomainEvents
{
    /// <summary>
    /// Gets the list of unpublished domain events.
    /// </summary>
    IReadOnlyList<DomainEvent> DomainEvents { get; }

    /// <summary>
    /// Marks the domain events as published.
    /// The list of domain events will be cleared.
    /// </summary>
    void ClearPublishedDomainEvents();

    /// <summary>
    /// Gets the id of the aggregate.
    /// </summary>
    Guid GetAggregateIdForDomainEvents();
}
