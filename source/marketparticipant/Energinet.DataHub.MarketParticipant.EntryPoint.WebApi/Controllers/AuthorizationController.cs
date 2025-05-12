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
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using IAuthorizationService = Microsoft.AspNetCore.Authorization.IAuthorizationService;

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
        IUserContext<FrontendUser> userContext,
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
    public async Task<ActionResult> CreateSignatureAsync([FromBody] string validationRequestJson)
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

        return Ok(result);
    }
}
