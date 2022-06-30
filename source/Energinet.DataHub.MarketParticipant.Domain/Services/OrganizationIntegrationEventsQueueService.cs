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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class OrganizationIntegrationEventsQueueService : IOrganizationIntegrationEventsQueueService
    {
        private readonly IDomainEventRepository _domainEventRepository;

        public OrganizationIntegrationEventsQueueService(
            IDomainEventRepository domainEventRepository)
        {
            _domainEventRepository = domainEventRepository;
        }

        public async Task EnqueueOrganizationIntegrationEventsAsync(IEnumerable<IIntegrationEvent> changeEvents)
        {
            ArgumentNullException.ThrowIfNull(changeEvents, nameof(changeEvents));

            foreach (var changeEvent in changeEvents)
            {
                var domainObjectId = FindDomainObjectId(changeEvent);

                var domainEvent = new DomainEvent(domainObjectId, nameof(Organization), changeEvent);
                await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
            }
        }

        private static Guid FindDomainObjectId(IIntegrationEvent changeEvent)
        {
            var domainId = changeEvent switch
            {
                OrganizationCreatedIntegrationEvent orgCreatedEvent => orgCreatedEvent.OrganizationId.Value,
                OrganizationAddressChangedIntegrationEvent addressChangedIntegrationEvent => addressChangedIntegrationEvent.OrganizationId.Value,
                OrganizationCommentChangedIntegrationEvent commentChangedIntegrationEvent => commentChangedIntegrationEvent.OrganizationId.Value,
                OrganizationNameChangedIntegrationEvent nameChangedIntegrationEvent => nameChangedIntegrationEvent.OrganizationId.Value,
                OrganizationBusinessRegisterIdentifierChangedIntegrationEvent businessRegisterIdentifierChangedEvent => businessRegisterIdentifierChangedEvent.OrganizationId.Value,
                _ => throw new ArgumentException("Unknown domain event type")
            };

            return domainId;
        }
    }
}
