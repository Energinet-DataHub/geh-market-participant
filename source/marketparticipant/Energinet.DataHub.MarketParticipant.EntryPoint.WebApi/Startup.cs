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

using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.Core.App.WebApp.Extensions.DependencyInjection;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public class Startup : Common.StartupBase
{
    /// <summary>
    /// Disables validation of external token and CreatedOn limit for KeyVault keys.
    /// This property is intended for testing purposes only.
    /// </summary>
    public static bool EnableIntegrationTestKeys { get; set; }

    protected override void Configure(IConfiguration configuration, IServiceCollection services)
    {
        services
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddScoped<IAuditIdentityProvider, FrontendUserAuditIdentityProvider>();

        services.AddOptions();
        services.AddOptions<UserAuthentication>().BindConfiguration(nameof(UserAuthentication)).ValidateDataAnnotations();

        services.AddSingleton<IExternalTokenValidator>(sp =>
        {
            var authSettings = sp.GetRequiredService<IOptions<UserAuthentication>>().Value;
            return new ExternalTokenValidator(
                authSettings.ExternalMetadataAddress.ToString(),
                authSettings.BackendBffAppId);
        });

        services.AddSingleton<ISigningKeyRing>(_ =>
        {
            var tokenKeyVaultUri = configuration.GetSetting(Settings.TokenKeyVault);
            var tokenKeyName = configuration.GetSetting(Settings.TokenKeyName);

            var tokenCredentials = new DefaultAzureCredential();

            var keyClient = new KeyClient(tokenKeyVaultUri, tokenCredentials);
            return new SigningKeyRing(Clock.Instance, keyClient, tokenKeyName);
        });

        services.AddSingleton(_ =>
        {
            var certificateKeyVaultUri = configuration.GetSetting(Settings.CertificateKeyVault);
            var defaultCredentials = new DefaultAzureCredential();
            return new SecretClient(certificateKeyVaultUri, defaultCredentials);
        });

        services.AddSingleton<ICertificateService>(s =>
        {
            var certificateClient = s.GetRequiredService<SecretClient>();
            var logger = s.GetRequiredService<ILogger<CertificateService>>();
            var certificateValidation = s.GetRequiredService<ICertificateValidation>();
            return new CertificateService(certificateClient, certificateValidation, logger);
        });

        SetupAuthentication(configuration, services);

        // Health check
        services
            .AddHealthChecks()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddCheck<GraphApiHealthCheck>("Graph API Access")
            .AddCheck<SigningKeyRingHealthCheck>("Signing Key Access")
            .AddCheck<CertificateKeyVaultHealthCheck>("Certificate Key Vault Access");
    }

    protected virtual void SetupAuthentication(IConfiguration configuration, IServiceCollection services)
    {
        services.AddJwtBearerAuthenticationForWebApp(configuration);
    }
}
