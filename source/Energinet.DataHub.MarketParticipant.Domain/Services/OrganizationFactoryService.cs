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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class OrganizationFactoryService : IOrganizationFactoryService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IGlobalLocationNumberUniquenessService _globalLocationNumberUniquenessService;
        private readonly IActiveDirectoryService _activeDirectoryService;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IDomainEventRepository _domainEventRepository;

        public OrganizationFactoryService(
            IOrganizationRepository organizationRepository,
            IGlobalLocationNumberUniquenessService globalLocationNumberUniquenessService,
            IActiveDirectoryService activeDirectoryService,
            IUnitOfWorkProvider unitOfWorkProvider,
            IDomainEventRepository domainEventRepository)
        {
            _organizationRepository = organizationRepository;
            _globalLocationNumberUniquenessService = globalLocationNumberUniquenessService;
            _activeDirectoryService = activeDirectoryService;
            _unitOfWorkProvider = unitOfWorkProvider;
            _domainEventRepository = domainEventRepository;
        }

        public async Task<Organization> CreateAsync(GlobalLocationNumber gln, string name)
        {
            await using var uow = await _unitOfWorkProvider.NewUnitOfWorkAsync().ConfigureAwait(false);

            await _globalLocationNumberUniquenessService
                .EnsureGlobalLocationNumberAvailableAsync(gln)
                .ConfigureAwait(false);

            var appRegistrationId = await _activeDirectoryService
                .EnsureAppRegistrationIdAsync(gln)
                .ConfigureAwait(false);

            var organizationToSave = new Organization(
                appRegistrationId,
                gln,
                name);

            var createdId = await _organizationRepository
                .AddOrUpdateAsync(organizationToSave)
                .ConfigureAwait(false);

            var organizationWithId = new Organization(
                createdId,
                organizationToSave.ActorId,
                organizationToSave.Gln,
                organizationToSave.Name,
                organizationToSave.Roles);

            var domainEvent = new OrganizationChangedIntegrationEvent
            {
                OrganizationId = organizationWithId.Id.Value,
                ActorId = organizationWithId.ActorId,
                Gln = organizationWithId.Gln.Value,
                Name = organizationWithId.Name
            };

            await _domainEventRepository.InsertAsync(new DomainEvent(organizationWithId.Id.Value, nameof(Organization), domainEvent)).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return organizationWithId;
        }
    }
}
