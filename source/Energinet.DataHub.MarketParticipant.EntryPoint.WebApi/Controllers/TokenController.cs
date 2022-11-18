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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IKeyClient _keyClient;
        private readonly ICryptographyClientProvider _cryptographyClientProvider;
        private readonly IMediator _mediator;

        public TokenController(IKeyClient keyClient, ICryptographyClientProvider cryptographyClientProvider, IMediator mediator)
        {
            _keyClient = keyClient;
            _cryptographyClientProvider = cryptographyClientProvider;
            _mediator = mediator;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("v2.0/.well-known/openid-configuration")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                issuer = "https://datahub.dk",
                jwks_uri = "http://localhost:6000/discovery/v2.0/keys",
            });
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("discovery/v2.0/keys")]
        public async Task<IActionResult> GetKeysAsync()
        {
            var keys = await _keyClient.GetKeysAsync().ConfigureAwait(false);

            var response = JsonSerializer.Serialize(
                new
                {
                    keys = keys.Select(
                        x => new
                        {
                            kid = x.Kid,
                            kty = x.Kty,
                            use = x.Use,
                            n = x.N,
                            e = x.E
                        }).ToArray()
                });

            return Ok(response);
        }

        [HttpPost]
        [Route("token")]
        public async Task<IActionResult> GetTokenAsync(TokenRequest tokenRequest)
        {
            if (tokenRequest is null)
                throw new ArgumentNullException(nameof(tokenRequest));

            var token = tokenRequest.ExternalToken;

            var key = await _keyClient.GetKeyAsync().ConfigureAwait(false);

            var jwtToken = new JwtSecurityToken(token);

            var newToken = new JwtSecurityToken(
                "https://datahub.dk",
                jwtToken.Audiences.Single(),
                new[]
                {
                    jwtToken.Claims.Single(x => x.Type == "sub"),
                    new Claim("token", token),
                    new Claim("role", "organization:view"),
                    new Claim("azp", tokenRequest.ActorId.ToString())
                },
                jwtToken.ValidFrom,
                jwtToken.ValidTo);

            newToken.Header["typ"] = "JWT";
            newToken.Header["alg"] = "RS256";
            newToken.Header["kid"] = key.Kid;

            var cryptoClient = _cryptographyClientProvider.GetClient(key.Id, new DefaultAzureCredential());

            var headerPayload = new JwtSecurityTokenHandler().WriteToken(newToken)[..^1];

            var result = await cryptoClient
                 .SignDataAsync(new SignatureAlgorithm(newToken.SignatureAlgorithm), Encoding.UTF8.GetBytes(headerPayload))
                 .ConfigureAwait(false);

            return Ok(new TokenResponse($"{headerPayload}.{Base64UrlEncoder.Encode(result.Signature)}"));
        }
    }
}
