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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;

/// <summary>
/// Handler with no input an d output, running every night to update the organization identity
/// </summary>
public class UpdateOrganizationIdentityTriggerHandler : IRequestHandler<UpdateOrganisationIdentityTriggerCommand, Unit>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationIdentityRepository _organizationIdentityRepository;
    private readonly ILogger<UpdateOrganizationIdentityTriggerHandler> _logger;

    public UpdateOrganizationIdentityTriggerHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationIdentityRepository organizationIdentityRepository,
        ILogger<UpdateOrganizationIdentityTriggerHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _organizationIdentityRepository = organizationIdentityRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateOrganisationIdentityTriggerCommand request, CancellationToken cancellationToken)
    {
        var alleOrganizations = await _organizationRepository.GetAsync().ConfigureAwait(false);

        foreach (var organization in alleOrganizations)
        {
            var organizationIdentity = await _organizationRepository.GetAsync(organization.Id).ConfigureAwait(false);

            var updateResponse = await _organizationIdentityRepository.GetAsync(organization.BusinessRegisterIdentifier).ConfigureAwait(false);

            if (updateResponse == null) continue;

            if (organizationIdentity!.Name != updateResponse.Name)
            {
                organizationIdentity.Name = updateResponse.Name;
                await _organizationRepository.AddOrUpdateAsync(organizationIdentity).ConfigureAwait(false);
                _logger.LogInformation($"Organization identity updated for organization with id {organization.Id} from {organizationIdentity.Name} to {updateResponse.Name}");
            }
        }

        _logger.LogInformation("Organization identity update completed");
        return Unit.Value;
    }
}
