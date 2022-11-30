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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Common.Security;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
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
        private readonly IUserContext<FrontendUser> _userContext;

        public ActorController(ILogger<ActorController> logger, IMediator mediator, IUserContext<FrontendUser> userContext)
        {
            _logger = logger;
            _mediator = mediator;
            _userContext = userContext;
        }

        [HttpGet("{organizationId:guid}/actor")]
        [AuthorizeUser(Permission.OrganizationView)]
        public async Task<IActionResult> GetActorsAsync(Guid organizationId)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (_userContext.CurrentUser.IsFasOrAssignedToOrganization(organizationId))
                        return Unauthorized();

                    var getActorsCommand = new GetActorsCommand(organizationId);

                    var response = await _mediator
                        .Send(getActorsCommand)
                        .ConfigureAwait(false);

                    return Ok(response.Actors);
                },
                _logger).ConfigureAwait(false);
        }

        [HttpGet("{organizationId:guid}/actor/{actorId:guid}")]
        [AuthorizeUser(Permission.OrganizationView)]
        public async Task<IActionResult> GetSingleActorAsync(Guid organizationId, Guid actorId)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                        return Unauthorized();

                    var getSingleActorCommand = new GetSingleActorCommand(actorId, organizationId);

                    var response = await _mediator
                        .Send(getSingleActorCommand)
                        .ConfigureAwait(false);

                    return Ok(response.Actor);
                },
                _logger).ConfigureAwait(false);
        }

        [HttpPost("{organizationId:guid}/actor")]
        [AuthorizeUser(Permission.OrganizationManage)]
        public async Task<IActionResult> CreateActorAsync(Guid organizationId, CreateActorDto actorDto)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFas)
                        return Unauthorized();

                    var createActorCommand = new CreateActorCommand(organizationId, actorDto);

                    var response = await _mediator
                        .Send(createActorCommand)
                        .ConfigureAwait(false);

                    return Ok(response.ActorId.ToString());
                },
                _logger).ConfigureAwait(false);
        }

        [HttpPut("{organizationId:guid}/actor/{actorId:guid}")]
        [AuthorizeUser(Permission.OrganizationManage)]
        public async Task<IActionResult> UpdateActorAsync(Guid organizationId, Guid actorId, ChangeActorDto changeActor)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                        return Unauthorized();

                    var updateActorCommand = new UpdateActorCommand(organizationId, actorId, changeActor);

                    var response = await _mediator
                        .Send(updateActorCommand)
                        .ConfigureAwait(false);

                    return Ok(response);
                },
                _logger).ConfigureAwait(false);
        }
    }
}
