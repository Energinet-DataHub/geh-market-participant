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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("actor")]
    public sealed class ActorContactController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserContext<FrontendUser> _userContext;

        public ActorContactController(IMediator mediator, IUserContext<FrontendUser> userContext)
        {
            _mediator = mediator;
            _userContext = userContext;
        }

        [HttpGet("{actorId:guid}/contact")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<IEnumerable<ActorContactDto>>> ListAllAsync(Guid actorId)
        {
            var getOrganizationsCommand = new GetActorContactsCommand(actorId);

            var response = await _mediator
                .Send(getOrganizationsCommand)
                .ConfigureAwait(false);

            return Ok(response.Contacts);
        }

        [HttpPost("{actorId:guid}/contact")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<Guid>> CreateContactAsync(Guid actorId, CreateActorContactDto contactDto)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var createContactCommand = new CreateActorContactCommand(actorId, contactDto);

            var response = await _mediator
                .Send(createContactCommand)
                .ConfigureAwait(false);

            return Ok(response.ContactId);
        }

        [HttpDelete("{actorId:guid}/contact/{contactId:guid}")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult> DeleteContactAsync(Guid actorId, Guid contactId)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var deleteContactCommand = new DeleteActorContactCommand(actorId, contactId);

            await _mediator
                .Send(deleteContactCommand)
                .ConfigureAwait(false);

            return Ok();
        }
    }
}
