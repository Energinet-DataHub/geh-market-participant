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
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("delegation")]
public sealed class DelegationController : ControllerBase
{
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public DelegationController(
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _userContext = userContext;
        _mediator = mediator;
    }

    [HttpGet("{actorId:guid}")]
    [AuthorizeUser(PermissionId.DelegationView)]
    public async Task<ActionResult<GetDelegationsForActorResponse>> GetDelegationsForActorAsync(Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(actorId);

        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var result = await _mediator
            .Send(new GetDelegationsForActorCommand(actorId))
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost]
    [AuthorizeUser(PermissionId.GridAreasManage)]
    public async Task<ActionResult<CreateDelegationResponse>> CreateDelegationAsync(CreateDelegationDto delegationDto)
    {
        var createDelegationCommand = new CreateDelegationCommand(delegationDto);

        var response = await _mediator
            .Send(createDelegationCommand)
            .ConfigureAwait(false);

        return Ok(response);
    }
}
