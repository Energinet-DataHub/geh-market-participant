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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.ActorIntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class ExternalActorSynchronizationService : IExternalActorSynchronizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IActorIntegrationEventsQueueService _actorIntegrationEventsQueueService;
    private readonly IExternalActorIdConfigurationService _externalActorIdConfigurationService;

    public ExternalActorSynchronizationService(
        IOrganizationRepository organizationRepository,
        IMarketParticipantDbContext marketParticipantDbContext,
        IActorIntegrationEventsQueueService actorIntegrationEventsQueueService,
        IExternalActorIdConfigurationService externalActorIdConfigurationService)
    {
        _organizationRepository = organizationRepository;
        _marketParticipantDbContext = marketParticipantDbContext;
        _actorIntegrationEventsQueueService = actorIntegrationEventsQueueService;
        _externalActorIdConfigurationService = externalActorIdConfigurationService;
    }

    public async Task ScheduleAsync(OrganizationId organizationId, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(actorId);

        var actorSync = new ActorSynchronizationEntity
        {
            OrganizationId = organizationId.Value,
            ActorId = actorId
        };

        await _marketParticipantDbContext
            .ActorSynchronizationEntries
            .AddAsync(actorSync)
            .ConfigureAwait(false);
    }

    public async Task SyncNextAsync()
    {
        var nextEntry = await GetNextEntityAsync().ConfigureAwait(false);
        if (nextEntry == null)
            return;

        var organization = await _organizationRepository
            .GetAsync(new OrganizationId(nextEntry.OrganizationId))
            .ConfigureAwait(false);

        var actor = organization!
            .Actors
            .First(actor => actor.Id == nextEntry.ActorId);

        // TODO: This service must be replaced with a reliable version in a future PR.
        await _externalActorIdConfigurationService
            .AssignExternalActorIdAsync(actor)
            .ConfigureAwait(false);

        await _organizationRepository
            .AddOrUpdateAsync(organization)
            .ConfigureAwait(false);

        await EnqueueExternalActorIdChangedEventAsync(organization.Id, actor).ConfigureAwait(false);

        RemoveEntity(nextEntry);
    }

    private Task<ActorSynchronizationEntity?> GetNextEntityAsync()
    {
        var query =
            from actorSync in _marketParticipantDbContext.ActorSynchronizationEntries
            orderby actorSync.Id
            select actorSync;

        return query.FirstOrDefaultAsync();
    }

    private void RemoveEntity(ActorSynchronizationEntity entity)
    {
        _marketParticipantDbContext.ActorSynchronizationEntries.Remove(entity);
    }

    private Task EnqueueExternalActorIdChangedEventAsync(OrganizationId organizationId, Actor actor)
    {
        var externalIdEvent = new ActorExternalIdChangedIntegrationEvent
        {
            OrganizationId = organizationId.Value,
            ActorId = actor.Id,
            ExternalActorId = actor.ExternalActorId?.Value
        };

        return _actorIntegrationEventsQueueService.EnqueueActorUpdatedEventAsync(
            organizationId,
            actor.Id,
            new IIntegrationEvent[] { externalIdEvent });
    }
}
