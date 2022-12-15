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
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class UserRoleAssignmentAuditLogController : ControllerBase
{
    private readonly ILogger<UserRoleAssignmentAuditLogController> _logger;
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public UserRoleAssignmentAuditLogController(
        ILogger<UserRoleAssignmentAuditLogController> logger,
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _logger = logger;
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet("auditlogs/userroles/user/{userId:guid}/actor/{actorId:guid}")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetAsync(Guid userId, Guid actorId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                    return Unauthorized();

                var command = new GetUserRoleAssignmentAuditLogsCommand(userId, actorId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response.UserRoleAssignmentAuditLogs);
            },
            _logger).ConfigureAwait(false);
    }
}
