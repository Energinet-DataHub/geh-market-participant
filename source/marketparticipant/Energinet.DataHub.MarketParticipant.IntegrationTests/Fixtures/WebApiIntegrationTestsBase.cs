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
using Energinet.DataHub.MarketParticipant.Common.Options;
using Energinet.DataHub.RevisionLog.Integration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

public abstract class WebApiIntegrationTestsBase<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    private const string ValidTestIssuer = "https://test.datahub.dk";

    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    protected WebApiIntegrationTestsBase(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    protected static string TestBackendAppId => "7C39AF16-AEA0-4B00-B4DB-D3E7B2D90A2E";

    protected static string CreateMockedTestToken(Guid userId, Guid actorId, params string[] permissions)
    {
        var roleClaims = permissions.Select(p => new Claim("role", p));

        var dataHubTokenClaims = roleClaims
            .Append(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()))
            .Append(new Claim(JwtRegisteredClaimNames.Azp, actorId.ToString()))
            .Append(new Claim("multitenancy", "true", ClaimValueTypes.Boolean));

        var dataHubToken = new JwtSecurityToken(
            ValidTestIssuer,
            TestBackendAppId,
            dataHubTokenClaims,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(10));

        return new JwtSecurityTokenHandler().WriteToken(dataHubToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Database:ConnectionString", _databaseFixture.DatabaseManager.ConnectionString);
        builder.UseSetting($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.MitIdExternalMetadataAddress)}", "fake_value");
        builder.UseSetting($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.ExternalMetadataAddress)}", "fake_value");
        builder.UseSetting($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.InternalMetadataAddress)}", "fake_value");
        builder.UseSetting($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.BackendBffAppId)}", TestBackendAppId);
        builder.UseSetting("KeyVault:TokenSignKeyVault", "https://fake_value");
        builder.UseSetting("KeyVault:TokenSignKeyName", "fake_value");
        builder.UseSetting("KeyVault:CertificatesKeyVault", "https://fake_value");
        builder.UseSetting("ServiceBusOptions:SharedIntegrationEventTopic", "fake_value");
        builder.UseSetting("ServiceBusOptions:IntegrationEventSubscription", "fake_value");
        builder.UseSetting("ServiceBusOptions:ProducerConnectionString", "fake_value");
        builder.UseSetting("ServiceBusOptions:HealthConnectionString", "fake_value");

        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication()
                .AddJwtBearer("bypass", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
#pragma warning disable CA5404
                        ValidIssuer = ValidTestIssuer,
                        ValidAudience = TestBackendAppId,
                        ValidateIssuerSigningKey = false,
                        ValidateLifetime = false,
                        RequireSignedTokens = false,
                        SignatureValidator = (t, _) => new JsonWebToken(t),
#pragma warning restore CA5404
                    };
                });

            var authorizationPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme, "bypass")
                .RequireAuthenticatedUser()
                .Build();

            services
                .AddAuthorizationBuilder()
                .SetDefaultPolicy(authorizationPolicy);

            services.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRevisionLogClient>().Object));
        });
    }
}
