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
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("user-roles")]
public sealed class UserRoleController : ControllerBase
{
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public UserRoleController(
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> GetAsync()
    {
        var command = new GetAllUserRolesCommand();

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Roles);
    }

    [HttpGet("{userRoleId:guid}")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> GetAsync(Guid userRoleId)
    {
        var command = new GetUserRoleCommand(userRoleId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Role);
    }

    [HttpPost]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    public async Task<IActionResult> CreateAsync(CreateUserRoleDto userRole)
    {
        var command = new CreateUserRoleCommand(_userContext.CurrentUser.UserId, userRole);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.UserRoleId.ToString());
    }

    [HttpPut("{userRoleId:guid}")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    public async Task<IActionResult> UpdateAsync(Guid userRoleId, UpdateUserRoleDto userRole)
    {
        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var command = new UpdateUserRoleCommand(_userContext.CurrentUser.UserId, userRoleId, userRole);

        await _mediator.Send(command).ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("{userRoleId:guid}/auditlogentry")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    public async Task<IActionResult> GetUserRoleAuditLogsAsync(Guid userRoleId)
    {
        var command = new GetUserRoleAuditLogsCommand(userRoleId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.UserRoleAuditLogs);
    }

    [HttpGet("permissions")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    public async Task<IActionResult> GetPermissionDetailsAsync(EicFunction eicFunction)
    {
        var command = new GetPermissionDetailsCommand(eicFunction);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Permissions);
    }

    [HttpGet("assignedtopermission")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    public async Task<IActionResult> AssignedToPermissionAsync(int permissionId)
    {
        var command = new GetUserRolesToPermissionCommand(permissionId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.UserRoles);
    }

    [HttpPut("{userRoleId:guid}/deactivate")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    public async Task<IActionResult> DeactivateUserRoleAsync(Guid userRoleId)
    {
        var command = new DeactivateUserRoleCommand(userRoleId, _userContext.CurrentUser.UserId);

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }
}
