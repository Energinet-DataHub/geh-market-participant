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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly ILogger<PermissionController> _logger;
        private readonly IUserContext<FrontendUser> _userContext;
        private readonly IMediator _mediator;

        public PermissionController(
            ILogger<PermissionController> logger,
            IUserContext<FrontendUser> userContext,
            IMediator mediator)
        {
            _logger = logger;
            _userContext = userContext;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> ListAllAsync()
        {
            return await this.ProcessAsync(
                async () =>
                {
                    var getPermissionsCommand = new GetPermissionsCommand();
                    var response = await _mediator
                        .Send(getPermissionsCommand)
                        .ConfigureAwait(false);
                    return Ok(response.Permissions);
                },
                _logger).ConfigureAwait(false);
        }

        [HttpPut]
        [AuthorizeUser(Permission.UserRoleManage)]
        public async Task<IActionResult> UpdateAsync(UpdatePermissionDto updatePermissionDto)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFas)
                        return Unauthorized();

                    var command = new UpdatePermissionCommand(_userContext.CurrentUser.UserId, updatePermissionDto.Id, updatePermissionDto.Description);

                    await _mediator
                        .Send(command)
                        .ConfigureAwait(false);

                    return Ok();
                },
                _logger).ConfigureAwait(false);
        }

        [HttpGet("{permissionId:int}/auditlogs")]
        [AuthorizeUser(Permission.UserRoleManage)]
        public async Task<IActionResult> GetAuditLogsAsync(int permissionId)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    var command = new GetPermissionAuditLogsCommand(permissionId);

                    var response = await _mediator
                        .Send(command)
                        .ConfigureAwait(false);

                    var logsFiltered = response.PermissionAuditLogs;

                    if (!_userContext.CurrentUser.IsFas)
                    {
                        logsFiltered = logsFiltered.Where(u => u.ChangedByUserId == _userContext.CurrentUser.UserId);
                    }

                    return Ok(logsFiltered);
                },
                _logger).ConfigureAwait(false);
        }
    }
}
