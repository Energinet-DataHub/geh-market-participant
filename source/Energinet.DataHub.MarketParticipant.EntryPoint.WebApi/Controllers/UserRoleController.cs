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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("user-roles")]
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

    [HttpGet]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAsync()
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new GetAllUserRolesCommand();

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Roles);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("{userRoleId:guid}")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAsync(Guid userRoleId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new GetUserRoleCommand(userRoleId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Role);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPost]
    [AuthorizeUser(Permission.UserRoleManage)]
    public async Task<IActionResult> CreateAsync(CreateUserRoleDto userRole)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new CreateUserRoleCommand(_userContext.CurrentUser.UserId, userRole);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.UserRoleId.ToString());
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPut("{userRoleId:guid}")]
    [AuthorizeUser(Permission.UserRoleManage)]
    public async Task<IActionResult> UpdateAsync(
        Guid userRoleId,
        UpdateUserRoleDto userRole)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFas)
                    return Unauthorized();

                var command = new UpdateUserRoleCommand(_userContext.CurrentUser.UserId, userRoleId, userRole);

                await _mediator.Send(command).ConfigureAwait(false);

                return Ok();
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("{userRoleId:guid}/auditlogentry")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetUserRoleAuditLogsAsync(Guid userRoleId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new GetUserRoleAuditLogsCommand(userRoleId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.UserRoleAuditLogs);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("permissions")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetSelectablePermissionsAsync(EicFunction eicFunction)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new GetSelectablePermissionsCommand(eicFunction);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.Permissions);
            },
            _logger).ConfigureAwait(false);
    }
}
