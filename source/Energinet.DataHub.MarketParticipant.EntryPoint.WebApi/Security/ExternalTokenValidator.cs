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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;

public sealed class ExternalTokenValidator : IExternalTokenValidator
{
    private readonly TokenValidationParameters _validationParameters;

    public ExternalTokenValidator(string metadataAddress, string audience)
    {
        _validationParameters = new TokenValidationParameters
        {
            ValidAudience = audience,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
            ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever()),
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (Startup.EnableIntegrationTestKeys)
            return true;

        var tokenHandler = new JwtSecurityTokenHandler();
        var result = await tokenHandler
            .ValidateTokenAsync(token, _validationParameters)
            .ConfigureAwait(false);

        return result.IsValid;
    }
}
