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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class ActorFactoryService : IActorFactoryService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IActorIntegrationEventsQueueService _actorIntegrationEventsQueueService;
        private readonly IOverlappingBusinessRolesRuleService _overlappingBusinessRolesRuleService;
        private readonly IUniqueGlobalLocationNumberRuleService _uniqueGlobalLocationNumberRuleService;
        private readonly IActiveDirectoryService _activeDirectoryService;

        public ActorFactoryService(
            IOrganizationRepository organizationRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IActorIntegrationEventsQueueService actorIntegrationEventsQueueService,
            IOverlappingBusinessRolesRuleService overlappingBusinessRolesRuleService,
            IUniqueGlobalLocationNumberRuleService uniqueGlobalLocationNumberRuleService,
            IActiveDirectoryService activeDirectoryService)
        {
            _organizationRepository = organizationRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _actorIntegrationEventsQueueService = actorIntegrationEventsQueueService;
            _overlappingBusinessRolesRuleService = overlappingBusinessRolesRuleService;
            _uniqueGlobalLocationNumberRuleService = uniqueGlobalLocationNumberRuleService;
            _activeDirectoryService = activeDirectoryService;
        }

        public async Task<Actor> CreateAsync(
            Organization organization,
            GlobalLocationNumber gln,
            IReadOnlyCollection<MarketRole> marketRoles,
            IReadOnlyCollection<MeteringPointType> meteringPointTypes)
        {
            Guard.ThrowIfNull(organization, nameof(organization));
            Guard.ThrowIfNull(gln, nameof(gln));
            Guard.ThrowIfNull(marketRoles, nameof(marketRoles));
            Guard.ThrowIfNull(marketRoles, nameof(meteringPointTypes));

            await _uniqueGlobalLocationNumberRuleService
                .ValidateGlobalLocationNumberAvailableAsync(organization, gln)
                .ConfigureAwait(false);

            _overlappingBusinessRolesRuleService.ValidateRolesAcrossActors(
                organization.Actors,
                marketRoles);

            var appRegistrationId = await _activeDirectoryService
                .EnsureAppRegistrationIdAsync(gln)
                .ConfigureAwait(false);

            var newActor = new Actor(appRegistrationId, gln);

            foreach (var marketRole in marketRoles)
                newActor.MarketRoles.Add(marketRole);

            foreach (var meteringPointType in meteringPointTypes.DistinctBy(e => e.Value))
                newActor.MeteringPointTypes.Add(meteringPointType);

            organization.Actors.Add(newActor);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            var savedActor = await SaveActorAsync(organization, appRegistrationId).ConfigureAwait(false);

            await _actorIntegrationEventsQueueService
                .EnqueueActorUpdatedEventAsync(organization.Id, savedActor)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return savedActor;
        }

        private async Task<Actor> SaveActorAsync(Organization organization, ExternalActorId appRegistrationId)
        {
            await _organizationRepository
                .AddOrUpdateAsync(organization)
                .ConfigureAwait(false);

            var savedOrganization = await _organizationRepository
                .GetAsync(organization.Id)
                .ConfigureAwait(false);

            return savedOrganization!
                .Actors
                .Single(a => a.ExternalActorId == appRegistrationId);
        }
    }
}
