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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;

public class OrganizationIdentityUpdateTrigger
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationIdentityUpdateTrigger> _logger;
    private readonly IFeatureManager _featureManager;

    public OrganizationIdentityUpdateTrigger(
        IMediator mediator,
        ILogger<OrganizationIdentityUpdateTrigger> logger,
        IFeatureManager featureManager)
    {
        _mediator = mediator;
        _logger = logger;
        _featureManager = featureManager;
    }

    [Function(nameof(OrganizationIdentityUpdateTrigger))]
    public async Task RunAsync([TimerTrigger("0 2 * * *")] FunctionContext context)
    {
        var isEnabled = await _featureManager.IsEnabledAsync("EnabledOrganizationIdentityUpdateTrigger").ConfigureAwait(false);

        if (isEnabled)
        {
            await _mediator.Send(new UpdateOrganisationIdentityTriggerCommand()).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation($"{nameof(OrganizationIdentityUpdateTrigger)} is disabled by feature flag.");
        }
    }
}
