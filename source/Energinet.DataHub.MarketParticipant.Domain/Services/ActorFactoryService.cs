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
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class ActorFactoryService : IActorFactoryService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IDomainEventRepository _domainEventRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IOverlappingBusinessRolesRuleService _overlappingBusinessRolesRuleService;
        private readonly IUniqueGlobalLocationNumberRuleService _uniqueGlobalLocationNumberRuleService;
        private readonly IBusinessRoleCodeDomainService _businessRoleCodeDomainService;
        private readonly IActiveDirectoryService _activeDirectoryService;

        public ActorFactoryService(
            IOrganizationRepository organizationRepository,
            IDomainEventRepository domainEventRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IOverlappingBusinessRolesRuleService overlappingBusinessRolesRuleService,
            IUniqueGlobalLocationNumberRuleService uniqueGlobalLocationNumberRuleService,
            IBusinessRoleCodeDomainService businessRoleCodeDomainService,
            IActiveDirectoryService activeDirectoryService)
        {
            _organizationRepository = organizationRepository;
            _domainEventRepository = domainEventRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _overlappingBusinessRolesRuleService = overlappingBusinessRolesRuleService;
            _uniqueGlobalLocationNumberRuleService = uniqueGlobalLocationNumberRuleService;
            _businessRoleCodeDomainService = businessRoleCodeDomainService;
            _activeDirectoryService = activeDirectoryService;
        }

        public async Task<Actor> CreateAsync(
            Organization organization,
            GlobalLocationNumber gln,
            IReadOnlyCollection<MarketRole> marketRoles)
        {
            Guard.ThrowIfNull(organization, nameof(organization));
            Guard.ThrowIfNull(gln, nameof(gln));
            Guard.ThrowIfNull(marketRoles, nameof(marketRoles));

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

            organization.Actors.Add(newActor);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await _organizationRepository
                .AddOrUpdateAsync(organization)
                .ConfigureAwait(false);

            var savedOrganization = await _organizationRepository
                .GetAsync(organization.Id)
                .ConfigureAwait(false);

            var savedActor = savedOrganization!.Actors.Single(a => a.ExternalActorId == appRegistrationId);

            var actorUpdatedEvent = new ActorUpdatedIntegrationEvent
            {
                OrganizationId = organization.Id,
                ActorId = savedActor.Id,
                ExternalActorId = savedActor.ExternalActorId,
                Gln = savedActor.Gln,
                Status = savedActor.Status
            };

            foreach (var marketRole in newActor.MarketRoles)
            {
                actorUpdatedEvent.MarketRoles.Add(marketRole.Function);
            }

            foreach (var businessRole in _businessRoleCodeDomainService.GetBusinessRoleCodes(newActor.MarketRoles))
            {
                actorUpdatedEvent.BusinessRoles.Add(businessRole);
            }

            var domainEvent = new DomainEvent(
                savedActor.Id,
                nameof(Actor),
                actorUpdatedEvent);

            await _domainEventRepository.InsertAsync(domainEvent).ConfigureAwait(false);
            await uow.CommitAsync().ConfigureAwait(false);

            return savedActor;
        }
    }
}
