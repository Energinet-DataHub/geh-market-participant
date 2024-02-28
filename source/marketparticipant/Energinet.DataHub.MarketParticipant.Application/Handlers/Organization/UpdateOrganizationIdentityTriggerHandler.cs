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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;

public class UpdateOrganizationIdentityTriggerHandler : IRequestHandler<UpdateOrganisationIdentityTriggerCommand>
{
    private static readonly CultureInfo _danishCulture = new("da-DK");

    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationIdentityRepository _organizationIdentityRepository;
    private readonly IEmailEventRepository _emailEventRepository;
    private readonly EmailRecipientConfig _emailRecipientConfig;
    private readonly ILogger<UpdateOrganizationIdentityTriggerHandler> _logger;

    public UpdateOrganizationIdentityTriggerHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationIdentityRepository organizationIdentityRepository,
        IEmailEventRepository emailEventRepository,
        EmailRecipientConfig emailRecipientConfig,
        ILogger<UpdateOrganizationIdentityTriggerHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _organizationIdentityRepository = organizationIdentityRepository;
        _emailEventRepository = emailEventRepository;
        _emailRecipientConfig = emailRecipientConfig;
        _logger = logger;
    }

    public async Task Handle(UpdateOrganisationIdentityTriggerCommand request, CancellationToken cancellationToken)
    {
        var allOrganizations = await _organizationRepository
            .GetAsync()
            .ConfigureAwait(false);

        var organizationsToCheck = allOrganizations
            .Where(e => !e.BusinessRegisterIdentifier.Identifier.StartsWith("ENDK", StringComparison.InvariantCultureIgnoreCase));

        foreach (var organization in organizationsToCheck)
        {
            var response = await _organizationIdentityRepository
                .GetAsync(organization.BusinessRegisterIdentifier)
                .ConfigureAwait(false);

            if (response == null)
            {
                continue;
            }

            var current = organization.Name.Trim();
            var newName = response.Name.Trim();

            if (_danishCulture.CompareInfo.Compare(current, newName) == 0)
            {
                continue;
            }

            organization.Name = newName;

            await _organizationRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);

            _logger.LogInformation($"Organization identity updated for organization with id {organization.Id} from {current} to {newName}");

            await SendNotificationEmailAsync(organization, current).ConfigureAwait(false);
        }
    }

    private Task SendNotificationEmailAsync(Domain.Model.Organization updatedOrganization, string oldOrganizationName)
    {
        return _emailEventRepository.InsertAsync(
            new EmailEvent(
                new EmailAddress(_emailRecipientConfig.OrgUpdateNotificationToEmail),
                new OrganizationIdentityChangedEmailTemplate(updatedOrganization, oldOrganizationName)));
    }
}
