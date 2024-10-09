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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.Common.Options;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Options;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public static class MarketParticipantWebApiModuleExtensions
{
    public static IServiceCollection AddMarketParticipantWebApiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarketParticipantCore();

        services
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddScoped<IAuditIdentityProvider, FrontendUserAuditIdentityProvider>();

        services.AddOptions<UserAuthentication>().BindConfiguration(nameof(UserAuthentication)).ValidateDataAnnotations();
        services.AddOptions<KeyVaultOptions>().BindConfiguration(KeyVaultOptions.SectionName).ValidateDataAnnotations();

        services.AddSingleton<IExternalTokenValidator>(sp =>
        {
            var authSettings = sp.GetRequiredService<IOptions<UserAuthentication>>().Value;
            return new ExternalTokenValidator(
                authSettings.ExternalMetadataAddress.ToString(),
                authSettings.BackendBffAppId);
        });

        services.AddSingleton<ISigningKeyRing>(provider =>
        {
            var tokenCredentials = new DefaultAzureCredential();
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            var keyClient = new KeyClient(options.Value.TokenSignKeyVault, tokenCredentials);
            return new SigningKeyRing(Clock.Instance, keyClient, options.Value.TokenSignKeyName);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            var defaultCredentials = new DefaultAzureCredential();
            return new SecretClient(options.Value.CertificatesKeyVault, defaultCredentials);
        });

        services.AddSingleton<ICertificateService>(s =>
        {
            var certificateClient = s.GetRequiredService<SecretClient>();
            var logger = s.GetRequiredService<ILogger<CertificateService>>();
            var certificateValidation = s.GetRequiredService<ICertificateValidation>();
            return new CertificateService(certificateClient, certificateValidation, logger);
        });

        // Health check
        services
            .AddHealthChecks()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddCheck<GraphApiHealthCheck>("Graph API Access")
            .AddCheck<SigningKeyRingHealthCheck>("Signing Key Access")
            .AddCheck<CertificateKeyVaultHealthCheck>("Certificate Key Vault Access");

        return services;
    }
}
