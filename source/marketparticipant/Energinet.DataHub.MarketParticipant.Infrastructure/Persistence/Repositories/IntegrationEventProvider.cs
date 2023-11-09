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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class IntegrationEventProvider : IIntegrationEventProvider
{
    private static readonly IReadOnlyDictionary<string, Type> _integrationEvents = typeof(DomainEvent)
        .Assembly
        .GetTypes()
        .Where(domainEventType => domainEventType.IsSubclassOf(typeof(DomainEvent)))
        .ToDictionary(x => x.Name);

    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IServiceProvider _serviceProvider;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public IntegrationEventProvider(
        IMarketParticipantDbContext marketParticipantDbContext,
        IServiceProvider serviceProvider)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public async IAsyncEnumerable<IntegrationEvent> GetAsync()
    {
        var domainEvents = await _marketParticipantDbContext
            .DomainEvents
            .Where(domainEvent => _integrationEvents.Keys.Contains(domainEvent.EventTypeName) && !domainEvent.IsSent)

            // Yes, we process events in opposite order, always taking the newest first.
            // This is allowed by contract and could help detect bugs in code assuming an order.
            // This only applies to integration events!
            .OrderByDescending(domainEvent => domainEvent.Timestamp)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var domainEventEntity in domainEvents)
        {
            var domainEvent = Map(domainEventEntity);

            yield return await CreateAsync((dynamic)domainEvent, domainEventEntity.Id).ConfigureAwait(false);

            domainEventEntity.IsSent = true;

            await _marketParticipantDbContext
                .SaveChangesAsync()
                .ConfigureAwait(false);
        }
    }

    private DomainEvent Map(DomainEventEntity domainEvent)
    {
        var deserializedDomainEvent = JsonSerializer.Deserialize(
            domainEvent.Event,
            _integrationEvents[domainEvent.EventTypeName],
            _jsonSerializerOptions);

        return (DomainEvent?)deserializedDomainEvent ??
               throw new InvalidOperationException($"Could not deserialize event {domainEvent.EventTypeName}.");
    }

    private Task<IntegrationEvent> CreateAsync<T>(T domainEvent, int sequenceNumber)
        where T : DomainEvent
    {
        var factoryService = _serviceProvider.GetRequiredService<IIntegrationEventFactory<T>>();
        return factoryService.CreateAsync(domainEvent, sequenceNumber);
    }
}
