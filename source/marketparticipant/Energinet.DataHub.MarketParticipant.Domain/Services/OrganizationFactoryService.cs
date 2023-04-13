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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class OrganizationFactoryService : IOrganizationFactoryService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IUniqueOrganizationBusinessRegisterIdentifierService _uniqueOrganizationBusinessRegisterIdentifierService;

        public OrganizationFactoryService(
            IOrganizationRepository organizationRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IUniqueOrganizationBusinessRegisterIdentifierService uniqueOrganizationBusinessRegisterIdentifierService)
        {
            _organizationRepository = organizationRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _uniqueOrganizationBusinessRegisterIdentifierService = uniqueOrganizationBusinessRegisterIdentifierService;
        }

        public async Task<Organization> CreateAsync(
            string name,
            BusinessRegisterIdentifier businessRegisterIdentifier,
            Address address,
            OrganizationDomain domain,
            string? comment)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(businessRegisterIdentifier, nameof(businessRegisterIdentifier));
            ArgumentNullException.ThrowIfNull(address, nameof(address));

            var newOrganization = new Organization(name, businessRegisterIdentifier, address, domain, comment);

            await _uniqueOrganizationBusinessRegisterIdentifierService
                .EnsureUniqueBusinessRegisterIdentifierAsync(newOrganization).ConfigureAwait(false);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            var savedOrganization = await SaveOrganizationAsync(newOrganization).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return savedOrganization;
        }

        private async Task<Organization> SaveOrganizationAsync(Organization organization)
        {
            var result = await _organizationRepository
                .AddOrUpdateAsync(organization)
                .ConfigureAwait(false);

            result.ThrowOnError(OrganizationErrorHandler.HandleOrganizationError);

            var savedOrganization = await _organizationRepository
                .GetAsync(result.Value)
                .ConfigureAwait(false);

            return savedOrganization!;
        }
    }
}
