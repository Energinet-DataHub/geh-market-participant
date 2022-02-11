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
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, CreateOrganisationResponse>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ILogger<CreateOrganizationHandler> _logger;

        public CreateOrganizationHandler(
            IOrganizationRepository organizationRepository,
            ILogger<CreateOrganizationHandler> logger)
        {
            _organizationRepository = organizationRepository;
            _logger = logger;
        }
        public async Task<CreateOrganisationResponse> Handle(
            CreateOrganizationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var organisationToSave = new Organization(
                    new Uuid(Guid.NewGuid()),
                    new GlobalLocationNumber(request.Gln),
                    request.Name);

                await _organizationRepository.SaveAsync(organisationToSave).ConfigureAwait(false);

                return new CreateOrganisationResponse(true, string.Empty);
            }
            catch (Exception e)
            {
                _logger.LogError("Error in CreateOrganizationHandler: {message}", e.Message);
                return new CreateOrganisationResponse(false, e.Message);
            }
        }
    }
}
