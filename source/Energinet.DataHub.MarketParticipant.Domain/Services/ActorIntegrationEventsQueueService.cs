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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.ActorIntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.GridAreaIntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class ActorIntegrationEventsQueueService : IActorIntegrationEventsQueueService
    {
        private readonly IDomainEventRepository _domainEventRepository;

        public ActorIntegrationEventsQueueService(IDomainEventRepository domainEventRepository)
        {
            _domainEventRepository = domainEventRepository;
        }

        public Task EnqueueActorUpdatedEventAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));

            var actorUpdatedEvent = new ActorUpdatedIntegrationEvent
            {
                OrganizationId = actor.OrganizationId.Value,
                ActorId = actor.Id.Value,
                ExternalActorId = actor.ExternalActorId,
                ActorNumber = new ActorNumberEventData(actor.ActorNumber.Value, actor.ActorNumber.Type),
                Status = actor.Status,
            };

            foreach (var actorMarketRole in actor.MarketRoles)
            {
                actorUpdatedEvent.ActorMarketRoles.Add(
                    new ActorMarketRoleEventData(
                        actorMarketRole.Function,
                        actorMarketRole.GridAreas.Select(
                            x => new ActorGridAreaEventData(
                                x.Id.Value,
                                x.MeteringPointTypes.Select(y => y.ToString()).ToList()))
                            .ToList()));
            }

            var domainEvent = new DomainEvent(actor.Id.Value, nameof(Actor), actorUpdatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }

        public Task EnqueueActorCreatedEventsAsync(Actor actor)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));

            var actorCreatedEvent = new ActorCreatedIntegrationEvent
            {
                OrganizationId = actor.OrganizationId,
                ActorId = actor.Id.Value,
                Status = actor.Status,
                ActorNumber = new ActorNumberEventData(actor.ActorNumber.Value, actor.ActorNumber.Type),
                Name = actor.Name
            };

            foreach (var actorMarketRole in actor.MarketRoles)
            {
                actorCreatedEvent.ActorMarketRoles.Add(
                    new ActorMarketRoleEventData(
                        actorMarketRole.Function,
                        actorMarketRole.GridAreas.Select(
                                x => new ActorGridAreaEventData(
                                    x.Id.Value,
                                    x.MeteringPointTypes.Select(y => y.ToString()).ToList()))
                            .ToList()));
            }

            var domainEvent = new DomainEvent(actor.Id.Value, nameof(Actor), actorCreatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }

        public async Task EnqueueActorUpdatedEventAsync(ActorId actorId, IEnumerable<IIntegrationEvent> integrationEvents)
        {
            ArgumentNullException.ThrowIfNull(actorId, nameof(actorId));
            ArgumentNullException.ThrowIfNull(integrationEvents, nameof(integrationEvents));

            foreach (var integrationEvent in integrationEvents)
            {
                switch (integrationEvent)
                {
                    case ActorStatusChangedIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId.Value, nameof(Actor), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case ActorNameChangedIntegrationEvent:
                    case ActorExternalIdChangedIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId.Value, nameof(Actor), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case MarketRoleAddedToActorIntegrationEvent or MarketRoleRemovedFromActorIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId.Value, nameof(Actor), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case GridAreaAddedToActorIntegrationEvent or GridAreaRemovedFromActorIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId.Value, nameof(Actor), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case MeteringPointTypeAddedToActorIntegrationEvent or MeteringPointTypeRemovedFromActorIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId.Value, nameof(Actor), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    default:
                        throw new InvalidOperationException(
                            $"Type of integration event '{integrationEvent.GetType()}' does not match valid event types.");
                }
            }
        }

        public Task EnqueueContactAddedToActorEventAsync(Actor actor, ActorContact contact)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));
            ArgumentNullException.ThrowIfNull(contact, nameof(contact));

            var actorCreatedEvent = new ContactAddedToActorIntegrationEvent
            {
                OrganizationId = actor.OrganizationId,
                ActorId = actor.Id.Value,
                Contact = new ActorContactEventData(contact.Name, contact.Email, contact.Category, contact.Phone)
            };

            var domainEvent = new DomainEvent(actor.Id.Value, nameof(Actor), actorCreatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }

        public Task EnqueueContactRemovedFromActorEventAsync(Actor actor, ActorContact contact)
        {
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));
            ArgumentNullException.ThrowIfNull(contact, nameof(contact));

            var actorCreatedEvent = new ContactRemovedFromActorIntegrationEvent
            {
                OrganizationId = actor.OrganizationId,
                ActorId = actor.Id.Value,
                Contact = new ActorContactEventData(contact.Name, contact.Email, contact.Category, contact.Phone)
            };

            var domainEvent = new DomainEvent(actor.Id.Value, nameof(Actor), actorCreatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }
    }
}
