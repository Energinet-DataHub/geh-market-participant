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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class InvitationController : ControllerBase
{
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public InvitationController(
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpPost("users/invite")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> InviteUserAsync([FromBody] UserInvitationDto userInvitation)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(userInvitation.AssignedActor))
            return Unauthorized();

        await _mediator
            .Send(new InviteUserCommand(userInvitation, _userContext.CurrentUser.UserId))
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPut("users/{userId:guid}/reinvite")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> ReInviteUserAsync(Guid userId)
    {
        if (!_userContext.CurrentUser.IsFas)
        {
            var associatedActors = await _mediator
                .Send(new GetActorsAssociatedWithUserCommand(userId))
                .ConfigureAwait(false);

            if (!_userContext.CurrentUser.IsAssignedToActor(associatedActors.AdministratedBy) &&
                !associatedActors.ActorIds.Any(_userContext.CurrentUser.IsAssignedToActor))
            {
                return Unauthorized();
            }
        }

        await _mediator
            .Send(new ReInviteUserCommand(userId, _userContext.CurrentUser.UserId))
            .ConfigureAwait(false);

        return Ok();
    }
}
