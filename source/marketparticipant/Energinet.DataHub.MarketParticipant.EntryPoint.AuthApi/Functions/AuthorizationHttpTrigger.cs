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
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Functions;

public sealed class AuthorizationHttpTrigger
{
    private const string BlockSignatureAuthorizationFeatureKey = "BlockSignatureAuthorization";

    private readonly IFeatureManager _featureManager;
    private readonly IMediator _mediator;

    public AuthorizationHttpTrigger(
        IFeatureManager featureManager,
        IMediator mediator)
    {
        _featureManager = featureManager;
        _mediator = mediator;
    }

    [Function("CreateSignature")]
    public async Task CreateSignatureAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "createSignature")]
        string validationRequestJson,
        HttpRequestData httpRequest)
    {
        var blockSignatureAuthorization = await _featureManager
            .IsEnabledAsync(BlockSignatureAuthorizationFeatureKey)
            .ConfigureAwait(false);

        if (blockSignatureAuthorization)
            throw new UnauthorizedAccessException("Signature authorization is not allowed.");

        var command = new CreateSignatureCommand(validationRequestJson);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        // TODO: Set the response body with the result
    }
}

/*


        var command = new CreateSignatureCommand(validationRequestJson);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(result);

        */
