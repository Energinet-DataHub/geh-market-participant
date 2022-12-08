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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Common.Security;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class UserRoleTemplateController : ControllerBase
{
    private readonly ILogger<UserRoleTemplateController> _logger;
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public UserRoleTemplateController(
        ILogger<UserRoleTemplateController> logger,
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _logger = logger;
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet("actors/{actorId:guid}/users/{userId:guid}/templates")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAsync(Guid actorId, Guid userId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                    return Unauthorized();

                var command = new GetUserRoleTemplatesCommand(userId, actorId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Templates);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("actors/{actorId:guid}/templates")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAssignableAsync(Guid actorId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                    return Unauthorized();

                var command = new GetAvailableUserRoleTemplatesForActorCommand(actorId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Templates);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPut("users/{userId:guid}/userroles")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> UpdateUserRoleAssignmentsAsync(Guid userId, UpdateUserRoleAssignmentsDto userRoleAssignmentsDto)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(userRoleAssignmentsDto);
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(userRoleAssignmentsDto.ActorId))
                    return Unauthorized();

                var result = await _mediator
                    .Send(new UpdateUserRoleAssignmentsCommand(userId, userRoleAssignmentsDto))
                    .ConfigureAwait(false);

                return Ok(result);
            },
            _logger).ConfigureAwait(false);
    }
}
