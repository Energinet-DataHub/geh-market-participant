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
using System.Linq;
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
        public async Task<IActionResult> GetActorsAsync()
        {
            var getAllActorsCommand = new GetAllActorsCommand();

            var response = await _mediator
                .Send(getAllActorsCommand)
                .ConfigureAwait(false);

            if (!_userContext.CurrentUser.IsFas)
            {
                var filteredActors = response
                    .Actors
                    .Select(a =>
                    {
                        if (_userContext.CurrentUser.IsAssignedToActor(a.ActorId))
                            return a;

                        return a with
                        {
                            MarketRoles = a
                                .MarketRoles
                                .Select(mr => mr with
                                {
                                    Comment = null,
                                    GridAreas = Array.Empty<ActorGridAreaDto>()
                                })
                        };
                    });

                return Ok(new GetActorsResponse(filteredActors));
            }

            return Ok(response.Actors);
        }

        [HttpGet("{actorId:guid}")]
        public async Task<IActionResult> GetSingleActorAsync(Guid actorId)
        {
            var getSingleActorCommand = new GetSingleActorCommand(actorId);

            var response = await _mediator
                .Send(getSingleActorCommand)
                .ConfigureAwait(false);

            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            {
                return Ok(new GetSingleActorResponse(response.Actor with
                {
                    MarketRoles = response
                        .Actor
                        .MarketRoles
                        .Select(mr => mr with
                        {
                            Comment = null,
                            GridAreas = Array.Empty<ActorGridAreaDto>()
                        })
                }));
            }

            return Ok(response.Actor);
        }

        [HttpPost]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<IActionResult> CreateActorAsync(CreateActorDto actorDto)
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
        public async Task<IActionResult> UpdateActorAsync(Guid actorId, ChangeActorDto changeActor)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var updateActorCommand = new UpdateActorCommand(actorId, changeActor);

            await _mediator
                .Send(updateActorCommand)
                .ConfigureAwait(false);

            return Ok();
        }
    }
}
