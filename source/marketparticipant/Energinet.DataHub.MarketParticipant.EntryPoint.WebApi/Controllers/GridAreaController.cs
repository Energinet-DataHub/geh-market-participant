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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class GridAreaController : ControllerBase
{
    private readonly IMediator _mediator;

    public GridAreaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [EnableRevision(RevisionActivities.PublicGridAreasRetrieved, typeof(GridArea))]
    public async Task<ActionResult<IEnumerable<GridAreaDto>>> GetGridAreasAsync()
    {
        var command = new GetGridAreasCommand();
        var response = await _mediator.Send(command).ConfigureAwait(false);
        return Ok(response.GridAreas);
    }

    [HttpPost]
    [EnableRevision(RevisionActivities.PublicGridAreasRetrieved, typeof(GridArea))]
    public async Task<ActionResult<IEnumerable<GridAreaDto>>> GetRelevantGridAreasAsync(GetRelevantGridAreasRequestDto getRelevantGridAreasRequest)
    {
        ArgumentNullException.ThrowIfNull(getRelevantGridAreasRequest);

        var command = new GetRelevantGridAreasCommand(getRelevantGridAreasRequest.Period);
        var response = await _mediator.Send(command).ConfigureAwait(false);
        return Ok(response.GridAreas);
    }

    [HttpGet("{gridAreaId:guid}")]
    [EnableRevision(RevisionActivities.PublicGridAreasRetrieved, typeof(GridArea), "gridAreaId")]
    public async Task<ActionResult<GridAreaDto>> GetGridAreaAsync(Guid gridAreaId)
    {
        var command = new GetGridAreaCommand(gridAreaId);
        var response = await _mediator.Send(command).ConfigureAwait(false);
        return Ok(response.GridArea);
    }

    [HttpGet("{gridAreaId:guid}/audit")]
    [EnableRevision(RevisionActivities.GridAreaAuditLogViewed, typeof(GridArea), "gridAreaId")]
    public async Task<ActionResult<IEnumerable<AuditLogDto<GridAreaAuditedChange>>>> GetAuditAsync(Guid gridAreaId)
    {
        var command = new GetGridAreaAuditLogsCommand(gridAreaId);
        var response = await _mediator.Send(command).ConfigureAwait(false);
        return Ok(response.AuditLogs);
    }
}
