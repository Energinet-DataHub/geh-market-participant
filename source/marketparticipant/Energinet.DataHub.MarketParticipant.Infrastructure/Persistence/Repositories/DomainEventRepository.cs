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
    }

    public async Task EnqueueAsync<T>(T aggregate)
        where T : IPublishDomainEvents
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        if (aggregate.DomainEvents.Count == 0)
            return;

        var aggregateId = aggregate.GetAggregateIdForDomainEvents();

        foreach (var domainEvent in aggregate.DomainEvents)
        {
            await _marketParticipantDbContext
                .DomainEvents
                .AddAsync(Map(aggregateId, typeof(T), domainEvent))
                .ConfigureAwait(false);
        }

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);

        aggregate.ClearPublishedDomainEvents();
    }

    private DomainEventEntity Map(Guid aggregateId, Type aggregateType, DomainEvent domainEvent)
    {
        return new DomainEventEntity
        {
            EntityId = aggregateId,
            EntityType = aggregateType.Name,
            Timestamp = DateTimeOffset.UtcNow,
            EventTypeName = domainEvent.GetType().Name,
            Event = JsonSerializer.Serialize(domainEvent, _jsonSerializerOptions)
        };
    }
}
