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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class GridAreaOverviewController : ControllerBase
{
    private readonly IMediator _mediator;

    public GridAreaOverviewController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [EnableRevision(RevisionActivities.PublicGridAreasRetrieved, typeof(GridArea))]
    public async Task<ActionResult<IEnumerable<GridAreaOverviewItemDto>>> GetGridAreaOverviewAsync()
    {
        var command = new GetGridAreaOverviewCommand();
        var response = await _mediator.Send(command).ConfigureAwait(false);
        return Ok(response.GridAreas);
    }
}
