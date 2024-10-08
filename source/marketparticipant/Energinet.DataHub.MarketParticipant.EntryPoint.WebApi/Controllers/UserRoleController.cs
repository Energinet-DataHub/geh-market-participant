﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
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
    [EnableRevision(RevisionActivities.AllUserRolesRetrieved, typeof(UserRole))]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetAsync()
    {
        var command = new GetAllUserRolesCommand();

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Roles);
    }

    [HttpGet("{userRoleId:guid}")]
    [AuthorizeUser(PermissionId.UsersManage)]
    [EnableRevision(RevisionActivities.UserRoleRetrieved, typeof(UserRole), "userRoleId")]
    public async Task<ActionResult<UserRoleWithPermissionsDto>> GetAsync(Guid userRoleId)
    {
        var command = new GetUserRoleCommand(userRoleId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Role);
    }

    [HttpGet("assignedtopermission")]
    [AuthorizeUser(PermissionId.UsersManage)]
    [EnableRevision(RevisionActivities.UserRolesAssignedToPermission, typeof(UserRole))]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> AssignedToPermissionAsync(int permissionId)
    {
        var command = new GetUserRolesToPermissionCommand(permissionId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.UserRoles);
    }

    [HttpPost]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.UserRoleCreated, typeof(UserRole))]
    public async Task<ActionResult<Guid>> CreateAsync(CreateUserRoleDto userRole)
    {
        var command = new CreateUserRoleCommand(userRole);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.UserRoleId);
    }

    [HttpPut("{userRoleId:guid}")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.UserRoleEdited, typeof(UserRole), "userRoleId")]
    public async Task<ActionResult> UpdateAsync(Guid userRoleId, UpdateUserRoleDto userRole)
    {
        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var command = new UpdateUserRoleCommand(userRoleId, userRole);

        await _mediator.Send(command).ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("{userRoleId:guid}/audit")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.UserRoleAuditLogViewed, typeof(UserRole), "userRoleId")]
    public async Task<ActionResult<IEnumerable<AuditLogDto<UserRoleAuditedChange>>>> GetAuditAsync(Guid userRoleId)
    {
        var command = new GetUserRoleAuditLogsCommand(userRoleId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.AuditLogs);
    }

    [HttpGet("permissions")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.PermissionDetailsViewed, typeof(Permission))]
    public async Task<ActionResult<IEnumerable<PermissionDetailsDto>>> GetPermissionDetailsAsync(EicFunction eicFunction)
    {
        var command = new GetPermissionDetailsCommand(eicFunction);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.Permissions);
    }

    [HttpPut("{userRoleId:guid}/deactivate")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.UserRoleDeactivated, typeof(UserRole), "userRoleId")]
    public async Task<ActionResult> DeactivateUserRoleAsync(Guid userRoleId)
    {
        var command = new DeactivateUserRoleCommand(userRoleId, _userContext.CurrentUser.UserId);

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }
}
