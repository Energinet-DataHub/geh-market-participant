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
using System.Net.Http;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.Core.App.FunctionApp.Diagnostics.HealthChecks;
using Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Monitor;
using Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Extensions.DependencyInjection;

internal static class ApimCertificateStoreExtensions
{
    public static IServiceCollection AddCertificateStore(this IServiceCollection services, IConfiguration configuration)
    {
        // register certificate store
        services.AddScoped<IKeyVaultCertificates, KeyVaultCertificates>();
        services.AddSingleton<IApimCertificateStore>(serviceProvider =>
        {
            var apimTenantId = configuration.GetValue<string>("APIM_TENANT_ID");
            var apimServicePrincipalClientId = configuration.GetValue<string>("APIM_SP_CLIENT_ID");
            var apimServicePrincipalClientSecret = configuration.GetValue<string>("APIM_SP_CLIENT_SECRET");

            var certificatesKeyVault = configuration.GetValue<Uri>("CERTIFICATES_KEY_VAULT");

            var apimCredentials = new ClientSecretCredential(
                apimTenantId,
                apimServicePrincipalClientId,
                apimServicePrincipalClientSecret);

            return new ApimCertificateStore(
                certificatesKeyVault,
                apimCredentials,
                serviceProvider.GetRequiredService<IHttpClientFactory>());
        });

        // register secret client
        services.AddSingleton(_ =>
        {
            var certificatesKeyVault = configuration.GetValue<Uri>("CERTIFICATES_KEY_VAULT");
            var defaultCredentials = new DefaultAzureCredential();
            return new SecretClient(certificatesKeyVault, defaultCredentials);
        });

        // specific health checks
        services.TryAddScoped<IHealthCheckEndpointHandler, HealthCheckEndpointHandler>();
        services.TryAddScoped<HealthCheckEndpoint>();

        services
            .AddHealthChecks()
            .AddCheck<ApimCertificateStoreHealthCheck>("APIM Certificate Access")
            .AddCheck<CertificateKeyVaultHealthCheck>("Certificate Key Vault Access");

        return services;
    }
}
