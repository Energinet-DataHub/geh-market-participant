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

using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
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
        public async Task<IActionResult> GetPermissionAsync(int permissionId)
        {
            var getPermissionCommand = new GetPermissionCommand(permissionId);
            var response = await _mediator
                .Send(getPermissionCommand)
                .ConfigureAwait(false);
            return Ok(response.Permission);
        }

        [HttpGet]
        public async Task<IActionResult> ListAllAsync()
        {
            var getPermissionsCommand = new GetPermissionsCommand();
            var response = await _mediator
                .Send(getPermissionsCommand)
                .ConfigureAwait(false);
            return Ok(response.Permissions);
        }

        [HttpPut]
        [AuthorizeUser(PermissionId.UserRolesManage)]
        public async Task<IActionResult> UpdateAsync(UpdatePermissionDto updatePermissionDto)
        {
            if (!_userContext.CurrentUser.IsFas)
                return Unauthorized();

            var command = new UpdatePermissionCommand(updatePermissionDto.Id, updatePermissionDto.Description);

            await _mediator
                .Send(command)
                .ConfigureAwait(false);

            return Ok();
        }

        [HttpGet("{permissionId:int}/auditlogs")]
        [AuthorizeUser(PermissionId.UserRolesManage)]
        public async Task<IActionResult> GetAuditLogsAsync(int permissionId)
        {
            var command = new GetPermissionAuditLogsCommand(permissionId);

            var response = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            return Ok(response.PermissionAuditLogs);
        }
    }
}
