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
                Gln = actor.Gln,
                Status = actor.Status
            };

            foreach (var marketRole in actor.MarketRoles)
            {
                actorUpdatedEvent.MarketRoles.Add(marketRole.Function);
            }

            foreach (var businessRole in _businessRoleCodeDomainService.GetBusinessRoleCodes(actor.MarketRoles))
            {
                actorUpdatedEvent.BusinessRoles.Add(businessRole);
            }

            foreach (var gridAreaId in actor.Areas)
            {
                actorUpdatedEvent.GridAreas.Add(gridAreaId);
            }

            foreach (var meteringPointTypes in actor.MeteringPointTypes)
            {
                actorUpdatedEvent.MeteringPointTypes.Add(meteringPointTypes.Name);
            }

            var domainEvent = new DomainEvent(actor.Id, nameof(Actor), actorUpdatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }
    }
}
