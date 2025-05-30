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
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class PermissionController : ControllerBase
{
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public PermissionController(
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet("{permissionId:int}")]
    [EnableRevision(RevisionActivities.PermissionViewed, typeof(Permission), "permissionId")]
    public async Task<ActionResult<PermissionDto>> GetPermissionAsync(int permissionId)
    {
        var getPermissionCommand = new GetPermissionCommand(permissionId);
        var response = await _mediator
            .Send(getPermissionCommand)
            .ConfigureAwait(false);
        return Ok(response.Permission);
    }

    [HttpGet]
    [EnableRevision(RevisionActivities.AllPermissionsViewed, typeof(Permission))]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> ListAllAsync()
    {
        var getPermissionsCommand = new GetPermissionsCommand();
        var response = await _mediator
            .Send(getPermissionsCommand)
            .ConfigureAwait(false);
        return Ok(response.Permissions);
    }

    [HttpPut("{permissionId:int}")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.PermissionEdited, typeof(Permission), "permissionId")]
    public async Task<ActionResult> UpdateAsync(int permissionId, UpdatePermissionDto updatePermissionDto)
    {
        ArgumentNullException.ThrowIfNull(updatePermissionDto);

        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var command = new UpdatePermissionCommand(permissionId, updatePermissionDto.Description);

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("{permissionId:int}/audit")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [EnableRevision(RevisionActivities.PermissionAuditLogViewed, typeof(Permission), "permissionId")]
    public async Task<ActionResult<IEnumerable<AuditLogDto<PermissionAuditedChange>>>> GetAuditAsync(int permissionId)
    {
        var command = new GetPermissionAuditLogsCommand(permissionId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.AuditLogs);
    }

    [HttpGet("relation")]
    [AuthorizeUser(PermissionId.UserRolesManage)]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [EnableRevision(RevisionActivities.PermissionOverview, typeof(Permission))]
    public async Task<ActionResult> GetPermissionsRelationAsync()
    {
        if (!_userContext.CurrentUser.IsFas)
        {
            return Unauthorized();
        }

        var command = new GetPermissionRelationsCommand();

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return File(response, "text/csv", "PermissionOverview.csv");
    }
}
