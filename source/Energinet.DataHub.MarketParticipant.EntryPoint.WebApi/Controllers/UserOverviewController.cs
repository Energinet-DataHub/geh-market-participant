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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class UserOverviewController : ControllerBase
{
    private readonly ILogger<UserOverviewController> _logger;
    private readonly IMediator _mediator;
    private readonly IUserContext<FrontendUser> _userContext;

    public UserOverviewController(ILogger<UserOverviewController> logger, IMediator mediator, IUserContext<FrontendUser> userContext)
    {
        _logger = logger;
        _mediator = mediator;
        _userContext = userContext;
    }

    [Obsolete("Changed to SearchUsersAsync.")]
    [HttpGet("users")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> GetUserOverviewAsync(int pageNumber, int pageSize, string? searchText, Application.Commands.Query.User.UserOverviewSortProperty sortProperty, SortDirection sortDirection, [FromQuery] IEnumerable<UserStatus> userStatus)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var actorId = !_userContext.CurrentUser.IsFas
                    ? _userContext.CurrentUser.ActorId
                    : (Guid?)null;

                var filter = new UserOverviewFilterDto(
                    actorId,
                    searchText,
                    sortProperty,
                    sortDirection,
                    Enumerable.Empty<Guid>(),
                    userStatus);

                var command = new GetUserOverviewCommand(filter, pageNumber, pageSize);
                var response = await _mediator.Send(command).ConfigureAwait(false);
                return Ok(response);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPost("users/search")]
    [AuthorizeUser(Permission.UsersManage)]
    public async Task<IActionResult> SearchUsersAsync(int pageNumber, int pageSize, Application.Commands.Query.User.UserOverviewSortProperty sortProperty, SortDirection sortDirection, [FromBody] UserOverviewFilterDto filter)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFas)
                {
                    if (filter.ActorId.HasValue && !_userContext.CurrentUser.IsAssignedToActor(filter.ActorId.Value))
                    {
                        return Unauthorized();
                    }

                    filter = filter with { ActorId = _userContext.CurrentUser.ActorId };
                }

                var command = new GetUserOverviewCommand(filter, pageNumber, pageSize);
                var response = await _mediator.Send(command).ConfigureAwait(false);
                return Ok(response);
            },
            _logger).ConfigureAwait(false);
    }
}
