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
using Energinet.DataHub.MarketParticipant.Application.Commands.AdditionalRecipients;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.DataApi.Functions;

internal sealed class AdditionalRecipientsHttpTrigger
{
    private readonly IMediator _mediator;

    public AdditionalRecipientsHttpTrigger(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function("GetAdditionalRecipientsAsync")]
    public async Task<IActionResult> GetAdditionalRecipientsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metering-point/{meteringPointId}/additionalRecipients")]
        HttpRequest httpRequest,
        string meteringPointId)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

        var command = new GetAdditionalRecipientsOfMeteringPointCommand(meteringPointId);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return new OkObjectResult(result.Recipients);
    }
}
