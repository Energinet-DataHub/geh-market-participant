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

using System.IdentityModel.Tokens.Jwt;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.LocalWebApi;

public class NoAuthStartup : WebApi.Startup
{
    public NoAuthStartup(IConfiguration configuration)
        : base(configuration)
    {
    }

    protected override void SetupAuthentication(IConfiguration configuration, IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var tokenValidationParameters = CreateValidationParameters();
                options.TokenValidationParameters = tokenValidationParameters;
            });
    }

    private static TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
#pragma warning disable CA5404
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false,
            RequireExpirationTime = false,
            RequireSignedTokens = false,
            SignatureValidator = (t, _) => new JwtSecurityToken(t),
#pragma warning restore CA5404
        };
    }
}
