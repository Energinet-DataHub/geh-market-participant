﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Manages domain events.
/// </summary>
public interface IDomainEventRepository
{
    /// <summary>
    /// Enqueue domain events for publishing from the provided aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate containing the domain events.</param>
    Task EnqueueAsync<T>(T aggregate)
        where T : IPublishDomainEvents;

    /// <summary>
    /// Enqueue domain events for publishing from the provided aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate containing the domain events.</param>
    /// <param name="aggregateId">The id of the aggregate containing the domain events.</param>
    Task EnqueueAsync<T>(T aggregate, Guid aggregateId)
        where T : IPublishDomainEvents;

    /// <summary>
    /// Enqueue a notification for publishing.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    Task EnqueueAsync(NotificationEvent notification);
}
