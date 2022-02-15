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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, Unit>
    {
        private readonly IOrganizationRepository _organizationRepository;

        public CreateOrganizationHandler(IOrganizationRepository organizationRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public async Task<Unit> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
        {
            Guard.ThrowIfNull(request, nameof(request));

            var organisationToSave = new Organization(
                new OrganizationId(Guid.NewGuid()),
                new GlobalLocationNumber(request.Gln),
                request.Name);

            await _organizationRepository.AddOrUpdateAsync(organisationToSave).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
