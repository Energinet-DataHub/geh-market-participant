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
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("organization")]
    public class ActorController : ControllerBase
    {
        private readonly ILogger<ActorController> _logger;
        private readonly IMediator _mediator;

        public ActorController(ILogger<ActorController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("{organizationId:guid}/actor/{actorId:guid}")]
        public async Task<IActionResult> GetSingleActorAsync(Guid organizationId, Guid actorId)
        {
            var getOrganizationsCommand = new GetSingleActorCommand(actorId, organizationId);

            var response = await _mediator
                .Send(getOrganizationsCommand)
                .ConfigureAwait(false);

            return response.ActorFound
                ? Ok(response)
                : NotFound();
        }

        [HttpPost("{organizationId:guid}/actor")]
        public async Task<IActionResult> CreateActorAsync(Guid organizationId, ChangeActorDto actorDto)
        {
            var getOrganizationsCommand = new CreateActorCommand(organizationId, actorDto);

            var response = await _mediator
                .Send(getOrganizationsCommand)
                .ConfigureAwait(false);

            return Ok(response);
        }
    }
}
