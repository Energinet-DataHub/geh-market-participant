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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrganizationController : ControllerBase
    {
        private readonly ILogger<OrganizationController> _logger;
        private readonly IMediator _mediator;

        public OrganizationController(ILogger<OrganizationController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> ListAllAsync()
        {
            var getOrganizationsCommand = new GetOrganizationsCommand();

            var response = await _mediator
                .Send(getOrganizationsCommand)
                .ConfigureAwait(false);

            return Ok(response);
        }

        [HttpGet("{organizationId:guid}")]
        public async Task<IActionResult> GetSingleOrganizationAsync(Guid organizationId)
        {
            var getSingleOrganizationCommand = new GetSingleOrganizationCommand(organizationId);

            var response = await _mediator
                .Send(getSingleOrganizationCommand)
                .ConfigureAwait(false);

            return response.OrganizationFound
                ? Ok(response)
                : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrganizationAsync(ChangeOrganizationDto organization)
        {
            var getSingleOrganizationCommand = new CreateOrganizationCommand(organization);

            var response = await _mediator
                .Send(getSingleOrganizationCommand)
                .ConfigureAwait(false);

            return Ok(response);
        }

        [HttpPut("{organizationId:guid}")]
        public async Task<IActionResult> UpdateOrganizationAsync(
            Guid organizationId,
            ChangeOrganizationDto organization)
        {
            var getSingleOrganizationCommand =
                new UpdateOrganizationCommand(organizationId, organization);

            var response = await _mediator
                .Send(getSingleOrganizationCommand)
                .ConfigureAwait(false);

            return Ok(response);
        }
    }
}
