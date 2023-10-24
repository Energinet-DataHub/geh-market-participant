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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class UserRoleAssignmentController : ControllerBase
{
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public UserRoleAssignmentController(
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet("actors/{actorId:guid}/users/{userId:guid}/roles")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetAsync(Guid actorId, Guid userId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var command = new GetUserRolesCommand(actorId, userId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Roles);
    }

    [HttpGet("actors/{actorId:guid}/roles")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetAssignableAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var command = new GetAvailableUserRolesForActorCommand(actorId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Roles);
    }

    [HttpPut("actors/{actorId:guid}/users/{userId:guid}/roles")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult> UpdateUserRoleAssignmentsAsync(
        Guid actorId,
        Guid userId,
        UpdateUserRoleAssignmentsDto assignments)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        await _mediator
            .Send(new UpdateUserRoleAssignmentsCommand(actorId, userId, assignments))
            .ConfigureAwait(false);

        return Ok();
    }
}
