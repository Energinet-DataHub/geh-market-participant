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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class DomainEventRepository : IDomainEventRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DomainEventRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public Task EnqueueAsync<T>(T aggregate)
        where T : IPublishDomainEvents
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var domainEvents = aggregate.DomainEvents;
        var aggregateId = domainEvents.GetAggregateIdForDomainEvents();

        return EnqueueAsync(aggregate, aggregateId);
    }

    public async Task EnqueueAsync<T>(T aggregate, Guid aggregateId)
        where T : IPublishDomainEvents
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var hasEvents = false;
        var domainEvents = aggregate.DomainEvents;

        foreach (var domainEvent in domainEvents)
        {
            hasEvents = true;

            await _marketParticipantDbContext
                .DomainEvents
                .AddAsync(Map(aggregateId, typeof(T), domainEvent))
                .ConfigureAwait(false);
        }

        if (!hasEvents)
            return;

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);

        domainEvents.ClearPublishedDomainEvents();
    }

    public async Task EnqueueAsync(NotificationEvent notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await _marketParticipantDbContext
            .DomainEvents
            .AddAsync(Map(notification.EventId, typeof(NotificationEvent), notification))
            .ConfigureAwait(false);

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    private DomainEventEntity Map(Guid aggregateId, Type aggregateType, DomainEvent domainEvent)
    {
        return new DomainEventEntity
        {
            EntityId = aggregateId,
            EntityType = aggregateType.Name,
            Timestamp = DateTimeOffset.UtcNow,
            EventTypeName = domainEvent.GetType().Name,
            Event = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonSerializerOptions)
        };
    }
}
