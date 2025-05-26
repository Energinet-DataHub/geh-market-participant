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

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Commands.Token;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Common.Options;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class TokenController : ControllerBase
{
    private const string Issuer = "https://datahub.dk";
    private const string RoleClaim = "role";
    private const string TokenClaim = "token";
    private const string ActorNumberClaim = "actornumber";
    private const string MarketRolesClaim = "marketroles";
    private const string GridAreasClaim = "gridareas";
    private const string MultiTenancyClaim = "multitenancy";

    private readonly IExternalTokenValidator _externalTokenValidator;
    private readonly ISigningKeyRing _signingKeyRing;
    private readonly IOptions<UserAuthentication> _authSettings;
    private readonly IMediator _mediator;

    internal TokenController(
        IExternalTokenValidator externalTokenValidator,
        ISigningKeyRing signingKeyRing,
        IOptions<UserAuthentication> authSettings,
        IMediator mediator)
    {
        _externalTokenValidator = externalTokenValidator;
        _signingKeyRing = signingKeyRing;
        _authSettings = authSettings;
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(".well-known/openid-configuration")]
    public IActionResult GetConfig()
    {
        var configuration = new
        {
            issuer = Issuer,
            jwks_uri = $"https://{Request.Host}/token/keys"
        };

        return Ok(configuration);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("token/keys")]
    public async Task<IActionResult> GetKeysAsync()
    {
        var jwks = await _signingKeyRing.GetKeysAsync().ConfigureAwait(false);
        var keys = new
        {
            keys = jwks.Select(
                jwk => new
                {
                    kid = GetKeyVersionIdentifier(jwk.Id),
                    kty = jwk.KeyType.ToString(),
                    n = jwk.N,
                    e = jwk.E
                })
        };

        return Ok(keys);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("token")]
    public async Task<ActionResult<TokenResponse>> GetTokenAsync(TokenRequest? tokenRequest)
    {
        if (tokenRequest == null || string.IsNullOrWhiteSpace(tokenRequest.ExternalToken))
            return BadRequest();

        var externalJwt = new JwtSecurityToken(tokenRequest.ExternalToken);

        if (!await _externalTokenValidator
                .ValidateTokenAsync(tokenRequest.ExternalToken)
                .ConfigureAwait(false))
        {
            return Unauthorized();
        }

        var externalUserId = GetExternalUserId(externalJwt.Claims);
        var actorId = tokenRequest.ActorId;
        var issuedAt = EpochTime.GetIntDate(DateTime.UtcNow);

        GetUserPermissionsResponse grantedPermissions;
        GetActorTokenDataResponse actorResponse;

        try
        {
            grantedPermissions = await _mediator
                .Send(new GetUserPermissionsCommand(externalUserId, actorId))
                .ConfigureAwait(false);

            actorResponse = await _mediator
                .Send(new GetActorTokenDataCommand(actorId))
                .ConfigureAwait(false);
        }
        catch (NotFoundValidationException)
        {
            return Unauthorized();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        var roleClaims = grantedPermissions.PermissionClaims
            .Select(p => new Claim(RoleClaim, p));

        var userId = grantedPermissions.UserId;

        var dataHubTokenClaims = roleClaims
            .Append(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()))
            .Append(new Claim(JwtRegisteredClaimNames.Azp, actorId.ToString()))
            .Append(new Claim(TokenClaim, tokenRequest.ExternalToken))
            .Append(new Claim(ActorNumberClaim, actorResponse.ActorTokenData.ActorNumber))
            .Append(new Claim(MarketRolesClaim, string.Join(',', actorResponse.ActorTokenData.MarketRoles.Select(x => x.Function))));

        if (actorResponse.ActorTokenData.MarketRoles.FirstOrDefault(x => x.Function == EicFunction.GridAccessProvider) is { } gridAccessProvider)
        {
            dataHubTokenClaims = dataHubTokenClaims
                .Append(new Claim(GridAreasClaim, string.Join(',', gridAccessProvider.GridAreas.Select(x => x.GridAreaCode))));
        }

        if (grantedPermissions.IsFas)
        {
            dataHubTokenClaims = dataHubTokenClaims
                .Append(new Claim(MultiTenancyClaim, "true", ClaimValueTypes.Boolean));
        }

        var dataHubToken = new JwtSecurityToken(
            Issuer,
            _authSettings.Value.BackendBffAppId,
            dataHubTokenClaims,
            externalJwt.ValidFrom,
            externalJwt.ValidTo);

        dataHubToken.Payload[JwtRegisteredClaimNames.Iat] = issuedAt;

        var finalToken = await CreateSignedTokenAsync(dataHubToken).ConfigureAwait(false);
        await _mediator.Send(new ClockUserLoginCommand(userId, SystemClock.Instance.GetCurrentInstant())).ConfigureAwait(false);
        return Ok(new TokenResponse(finalToken));
    }

    [HttpPost("createDownloadToken")]
    public async Task<ActionResult<Guid>> CreateDownloadTokenAsync()
    {
        var authToken = Request.Headers["Authorization"].ToString();
        var command = new CreateDownloadTokenCommand(authToken);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPost("exchangeDownloadToken/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<ExchangeDownloadTokenDto>> ExchangeDownloadTokenAsync(Guid token)
    {
        var command = new ExchangeDownloadTokenCommand(token);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response);
    }

    private static Guid GetExternalUserId(IEnumerable<Claim> claims)
    {
        var userIdClaim = claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub);
        return Guid.Parse(userIdClaim.Value);
    }

    private static string GetKeyVersionIdentifier(string key)
    {
        return key[(key.LastIndexOf('/') + 1)..];
    }

    private async Task<string> CreateSignedTokenAsync(JwtSecurityToken dataHubToken)
    {
        var signingClient = await _signingKeyRing
            .GetSigningClientAsync()
            .ConfigureAwait(false);

        dataHubToken.Header[JwtHeaderParameterNames.Typ] = JwtConstants.TokenType;
        dataHubToken.Header[JwtHeaderParameterNames.Alg] = _signingKeyRing.Algorithm;
        dataHubToken.Header[JwtHeaderParameterNames.Kid] = GetKeyVersionIdentifier(signingClient.KeyId);

        var headerAndPayload = new JwtSecurityTokenHandler().WriteToken(dataHubToken);

        var signResult = await signingClient
            .SignDataAsync(
                new SignatureAlgorithm(_signingKeyRing.Algorithm),
                Encoding.UTF8.GetBytes(headerAndPayload[..^1]))
            .ConfigureAwait(false);

        return headerAndPayload + Base64UrlEncoder.Encode(signResult.Signature);
    }
}
