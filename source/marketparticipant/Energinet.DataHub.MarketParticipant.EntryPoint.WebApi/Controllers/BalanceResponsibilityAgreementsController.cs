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
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("balance-responsibility-agreements")]
public class BalanceResponsibilityAgreementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext<FrontendUser> _userContext;

    public BalanceResponsibilityAgreementsController(IMediator mediator, IUserContext<FrontendUser> userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    [HttpGet("{actorId:guid}")]
    [AuthorizeUser(PermissionId.ActorsManage)]
    public async Task<ActionResult<IEnumerable<BalanceResponsibilityAgreementDto>>> GetBalanceResponsibilityAgreementsAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var result = await _mediator
            .Send(new GetBalanceResponsibilityAgreementsCommand(actorId))
            .ConfigureAwait(false);

        return Ok(result.Agreements);
    }
}
