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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("actor")]
    public class ActorController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserContext<FrontendUser> _userContext;

        public ActorController(IMediator mediator, IUserContext<FrontendUser> userContext)
        {
            _mediator = mediator;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActorDto>>> GetActorsAsync()
        {
            var getAllActorsCommand = new GetAllActorsCommand();

            var response = await _mediator
                .Send(getAllActorsCommand)
                .ConfigureAwait(false);

            return Ok(response.Actors);
        }

        [HttpGet("{actorId:guid}")]
        public async Task<ActionResult<ActorDto>> GetSingleActorAsync(Guid actorId)
        {
            var getSingleActorCommand = new GetSingleActorCommand(actorId);

            var response = await _mediator
                .Send(getSingleActorCommand)
                .ConfigureAwait(false);

            return Ok(response.Actor);
        }

        [HttpPost]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<string>> CreateActorAsync(CreateActorDto actorDto)
        {
            if (!_userContext.CurrentUser.IsFas)
                return Unauthorized();

            var createActorCommand = new CreateActorCommand(actorDto);

            var response = await _mediator
                .Send(createActorCommand)
                .ConfigureAwait(false);

            return Ok(response.ActorId.ToString());
        }

        [HttpPut("{actorId:guid}")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult> UpdateActorAsync(Guid actorId, ChangeActorDto changeActor)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var updateActorCommand = new UpdateActorCommand(actorId, changeActor);

            await _mediator
                .Send(updateActorCommand)
                .ConfigureAwait(false);

            return Ok();
        }

        [HttpGet("{actorId:guid}/auditlogs")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<GetActorAuditLogsResponse>> GetActorAuditLogsAsync(Guid actorId)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var command = new GetActorAuditLogsCommand(actorId);

            var response = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            return Ok(response);
        }
    }
}
