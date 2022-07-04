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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class ActorIntegrationEventsQueueService : IActorIntegrationEventsQueueService
    {
        private readonly IDomainEventRepository _domainEventRepository;
        private readonly IBusinessRoleCodeDomainService _businessRoleCodeDomainService;

        public ActorIntegrationEventsQueueService(
            IDomainEventRepository domainEventRepository,
            IBusinessRoleCodeDomainService businessRoleCodeDomainService)
        {
            _domainEventRepository = domainEventRepository;
            _businessRoleCodeDomainService = businessRoleCodeDomainService;
        }

        public Task EnqueueActorUpdatedEventAsync(OrganizationId organizationId, Actor actor)
        {
            ArgumentNullException.ThrowIfNull(organizationId, nameof(organizationId));
            ArgumentNullException.ThrowIfNull(actor, nameof(actor));

            var actorUpdatedEvent = new ActorUpdatedIntegrationEvent
            {
                OrganizationId = organizationId,
                ActorId = actor.Id,
                ExternalActorId = actor.ExternalActorId,
                ActorNumber = actor.ActorNumber,
                Status = actor.Status
            };

            foreach (var marketRole in actor.MarketRoles)
            {
                actorUpdatedEvent.MarketRoles.Add(marketRole.Function);
            }

            foreach (var businessRole in _businessRoleCodeDomainService.GetBusinessRoleCodes(actor.MarketRoles.Select(m => m.Function)))
            {
                actorUpdatedEvent.BusinessRoles.Add(businessRole);
            }

            // Temporary flattening the grid areas to use in the existing event
            var gridAreas = actor.MarketRoles
                .SelectMany(x => x.GridAreas
                    .Select(y => y.Id))
                .Distinct();
            foreach (var gridAreaId in gridAreas)
            {
                actorUpdatedEvent.GridAreas.Add(new GridAreaId(gridAreaId));
            }

            // Temporary flattening the metering point types to use in the existing event
            var meteringPoints = actor.MarketRoles
                .SelectMany(x => x.GridAreas
                    .SelectMany(y => y.MeteringPointTypes
                        .Select(z => z.Name)))
                .Distinct();
            foreach (var meteringPoint in meteringPoints)
            {
                actorUpdatedEvent.MeteringPointTypes.Add(meteringPoint);
            }

            var domainEvent = new DomainEvent(actor.Id, nameof(Actor), actorUpdatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }

        public async Task EnqueueActorUpdatedEventAsync(OrganizationId organizationId, Guid actorId, IEnumerable<IIntegrationEvent> integrationEvents)
        {
            ArgumentNullException.ThrowIfNull(organizationId, nameof(organizationId));
            ArgumentNullException.ThrowIfNull(integrationEvents, nameof(integrationEvents));

            foreach (var integrationEvent in integrationEvents)
            {
                switch (integrationEvent)
                {
                    case ActorStatusChangedIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId, nameof(ActorStatus), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case AddMarketRoleIntegrationEvent or RemoveMarketRoleIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId, nameof(ActorMarketRole), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case AddGridAreaIntegrationEvent or RemoveGridAreaIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId, nameof(ActorGridArea), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    case AddMeteringPointTypeIntegrationEvent or RemoveMeteringPointTypeIntegrationEvent:
                        {
                            var domainEvent = new DomainEvent(actorId, nameof(MeteringPointType), integrationEvent);
                            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
                            break;
                        }

                    default:
                        throw new InvalidOperationException(
                            $"Type of integration event '{integrationEvent.GetType()}' does not match valid event types.");
                }
            }
        }
    }
}
