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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("authorization")]
public class AuthorizationController : ControllerBase
{
    private const string BlockSignatureAuthorizationFeatureKey = "BlockSignatureAuthorization";

    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IFeatureManager _featureManager;

    public AuthorizationController(
        IAuthorizationService authorizationService,
        IFeatureManager featureManager,
        IMediator mediator)
    {
        _authorizationService = authorizationService;
        _featureManager = featureManager;
        _mediator = mediator;
    }

    [HttpPost("createSignature")]
    [AllowAnonymous]
    public async Task<ActionResult> CreateSignatureAsync([FromBody] string access)
    {
        var blockSignatureAuthorization = await _featureManager
            .IsEnabledAsync(BlockSignatureAuthorizationFeatureKey)
            .ConfigureAwait(false);

        if (blockSignatureAuthorization)
            throw new UnauthorizedAccessException("Signature authorization is not allowed.");

        var command = new CreateSignatureCommand(access);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("verifySignature")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifySignatureAsync(string signature)
    {
        var blockSignatureAuthorization = await _featureManager
            .IsEnabledAsync(BlockSignatureAuthorizationFeatureKey)
            .ConfigureAwait(false);

        if (blockSignatureAuthorization)
            throw new UnauthorizedAccessException("Signature authorization is not allowed.");

        var command = new VerifySignatureCommand(signature);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
