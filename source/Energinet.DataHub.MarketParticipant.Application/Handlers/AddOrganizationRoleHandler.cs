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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Roles;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public class AddOrganizationRoleHandler : IRequestHandler<AddOrganizationRoleCommand, Unit>
    {
        private readonly IOrganizationRepository _organizationRepository;

        public AddOrganizationRoleHandler(IOrganizationRepository organizationRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public async Task<Unit> Handle(AddOrganizationRoleCommand request, CancellationToken cancellationToken)
        {
            Guard.ThrowIfNull(request, nameof(request));

            var organizationId = new OrganizationId(Guid.Parse(request.OrganizationId));

            var organization = await _organizationRepository
                .GetAsync(organizationId)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new NotFoundValidationException(organizationId.Value);
            }

            organization.AddRole(CreateRole(request.Role));

            await _organizationRepository
                .AddOrUpdateAsync(organization)
                .ConfigureAwait(false);

            return Unit.Value;
        }

        private static IOrganizationRole CreateRole(OrganizationRoleDto organizationRoleDto)
        {
            var businessRole = Enum.Parse<BusinessRoleCode>(organizationRoleDto.BusinessRole, true);

            IOrganizationRole organizationRole = businessRole switch
            {
                BusinessRoleCode.Ddk => new BalanceResponsiblePartyRole(),
                BusinessRoleCode.Ddm => new GridAccessProviderRole(),
                BusinessRoleCode.Ddq => new BalancePowerSupplierRole(),
                BusinessRoleCode.Ddx => new ImbalanceSettlementResponsibleRole(),
                BusinessRoleCode.Ddz => new MeteringPointAdministratorRole(),
                BusinessRoleCode.Dea => new MeteredDataAggregatorRole(),
                BusinessRoleCode.Ez => new SystemOperatorRole(),
                BusinessRoleCode.Mdr => new MeteredDataResponsibleRole(),
                BusinessRoleCode.Sts => new DanishEnergyAgencyRole(),
                _ => throw new ArgumentOutOfRangeException(nameof(organizationRoleDto))
            };

            foreach (var marketRoleDto in organizationRoleDto.MarketRoles)
            {
                var function = Enum.Parse<EicFunction>(marketRoleDto.Function, true);
                organizationRole.MarketRoles.Add(new MarketRole(function));
            }

            return organizationRole;
        }
    }
}
