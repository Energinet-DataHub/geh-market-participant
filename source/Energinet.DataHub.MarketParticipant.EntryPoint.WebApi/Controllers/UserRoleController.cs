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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class UserRoleController : ControllerBase
{
    private readonly ILogger<UserRoleController> _logger;
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public UserRoleController(
        ILogger<UserRoleController> logger,
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _logger = logger;
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet("actors/{actorId:guid}/users/{userId:guid}/roles")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAsync(Guid actorId, Guid userId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                    return Unauthorized();

                var command = new GetUserRolesCommand(actorId, userId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Roles);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("actors/{actorId:guid}/roles")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAssignableAsync(Guid actorId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                    return Unauthorized();

                var command = new GetAvailableUserRolesForActorCommand(actorId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Roles);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPut("actors/{actorId:guid}/users/{userId:guid}/roles")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> UpdateUserRoleAssignmentsAsync(Guid actorId, Guid userId, UpdateUserRolesDto userRolesDto)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(userRolesDto);

        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                    return Unauthorized();

                var result = await _mediator
                    .Send(new UpdateUserRoleAssignmentsCommand(actorId, userId, userRolesDto))
                    .ConfigureAwait(false);

                return Ok(result);
            },
            _logger).ConfigureAwait(false);
    }
}
