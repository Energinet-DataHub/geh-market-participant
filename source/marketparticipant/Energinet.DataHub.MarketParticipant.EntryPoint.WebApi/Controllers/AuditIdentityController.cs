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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("audit-identity")]
public sealed class AuditIdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditIdentityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{auditIdentityId:guid}")]
    public async Task<ActionResult<GetAuditIdentityResponse>> GetAsync(Guid auditIdentityId)
    {
        // NOTE: There is no permission attribute, as command itself filters results.
        var response = await _mediator
            .Send(new GetAuditIdentityCommand(auditIdentityId))
            .ConfigureAwait(false);

        return Ok(response);
    }
}
