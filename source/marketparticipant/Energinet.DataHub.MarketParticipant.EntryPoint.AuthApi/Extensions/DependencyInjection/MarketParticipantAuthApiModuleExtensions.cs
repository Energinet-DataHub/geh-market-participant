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
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Options;
using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Monitor;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Extensions.DependencyInjection;

internal static class MarketParticipantAuthApiModuleExtensions
{
    public static IServiceCollection AddMarketParticipantAuthApiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarketParticipantCore();
        services.AddAuthorizationCore();

        services.AddScoped<IAuditIdentityProvider>(_ => KnownAuditIdentityProvider.AuthApiBackgroundService);
        services.AddFeatureManagement();

        services.AddOptions<KeyVaultOptions>().BindConfiguration(KeyVaultOptions.SectionName).ValidateDataAnnotations();

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            var defaultCredentials = new DefaultAzureCredential();
            return new SecretClient(options.Value.AuthSignKeyVault, defaultCredentials);
        });
        AddHealthChecks(services);

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddScoped<HealthCheckEndpoint>();
        services
            .AddHealthChecks()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddCheck<AuthSignKeyVaultHealthCheck>("Auth Sign Key Vault Access");
    }
}
