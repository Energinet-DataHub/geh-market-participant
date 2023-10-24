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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public class TokenController : ControllerBase
{
    private const string Issuer = "https://datahub.dk";
    private const string RoleClaim = "role";
    private const string TokenClaim = "token";
    private const string MembershipClaim = "membership";
    private const string FasMembership = "fas";
    private const string ActorNumberClaim = "actornumber";
    private const string MarketRolesClaim = "marketroles";

    private readonly IExternalTokenValidator _externalTokenValidator;
    private readonly ISigningKeyRing _signingKeyRing;
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;

    public TokenController(
        IExternalTokenValidator externalTokenValidator,
        ISigningKeyRing signingKeyRing,
        IConfiguration configuration,
        IMediator mediator)
    {
        _externalTokenValidator = externalTokenValidator;
        _signingKeyRing = signingKeyRing;
        _configuration = configuration;
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
        GetSingleActorResponse actorResponse;

        try
        {
            grantedPermissions = await _mediator
                .Send(new GetUserPermissionsCommand(externalUserId, actorId))
                .ConfigureAwait(false);

            actorResponse = await _mediator
                            .Send(new GetSingleActorCommand(actorId))
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

        var dataHubTokenClaims = roleClaims
            .Append(new Claim(JwtRegisteredClaimNames.Sub, grantedPermissions.UserId.ToString()))
            .Append(new Claim(JwtRegisteredClaimNames.Azp, actorId.ToString()))
            .Append(new Claim(TokenClaim, tokenRequest.ExternalToken))
            .Append(new Claim(ActorNumberClaim, actorResponse.Actor.ActorNumber.Value))
            .Append(new Claim(MarketRolesClaim, string.Join(',', actorResponse.Actor.MarketRoles.Select(x => x.EicFunction))));

        if (grantedPermissions.IsFas)
        {
            dataHubTokenClaims = dataHubTokenClaims.Append(new Claim(MembershipClaim, FasMembership));
        }

        var dataHubToken = new JwtSecurityToken(
            Issuer,
            _configuration.GetSetting(Settings.BackendBffAppId),
            dataHubTokenClaims,
            externalJwt.ValidFrom,
            externalJwt.ValidTo);

        dataHubToken.Payload[JwtRegisteredClaimNames.Iat] = issuedAt;

        var finalToken = await CreateSignedTokenAsync(dataHubToken).ConfigureAwait(false);
        return Ok(new TokenResponse(finalToken));
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
