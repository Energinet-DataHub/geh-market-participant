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
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Extensions.HealthChecks;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Factories;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Options;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Extensions.DependencyInjection;

public static class AddSignatureAuthorizationExtensions
{
    public static void AddSignatureAuthorizationCore(this IServiceCollection services)
    {
        services
            .AddOptions<ElectricityMarketClientOptions>()
            .BindConfiguration(ElectricityMarketClientOptions.SectionName)
            .ValidateDataAnnotations();

        services.TryAddSingleton<IAuthorizationHeaderProvider>(sp =>
        {
            // We currently register AuthorizationHeaderProvider like this to be in control of the
            // creation of DefaultAzureCredential.
            // As we register IAuthorizationHeaderProvider as singleton, and it has the instance
            // of DefaultAzureCredential, we expect it will use caching and handle token refresh.
            // However, the documentation is a bit unclear: https://learn.microsoft.com/da-dk/dotnet/azure/sdk/authentication/best-practices?tabs=aspdotnet#understand-when-token-lifetime-and-caching-logic-is-needed
            var credential = new DefaultAzureCredential();
            var options = sp.GetRequiredService<IOptions<ElectricityMarketClientOptions>>().Value;
            return new AuthorizationHeaderProvider(credential, options.ApplicationIdUri);
        });

        services.AddHttpClient("SignatureAuthElectricityMarketClient", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ElectricityMarketClientOptions>>();
            var headerProvider = provider.GetRequiredService<IAuthorizationHeaderProvider>();
            client.DefaultRequestHeaders.Authorization = headerProvider.CreateAuthorizationHeader();
            client.BaseAddress = options.Value.BaseUrl;
        });

        services.TryAddScoped<IElectricityMarketClient>(s =>
        {
            var client = s.GetRequiredService<IHttpClientFactory>().CreateClient("SignatureAuthElectricityMarketClient");
            return new ElectricityMarketClient(client);
        });

        services.AddSingleton<IAccessValidatorDispatchService, AccessValidatorDispatchService>();
        services.AddSingleton<IAccessValidator<MeteringPointMasterDataAccessValidationRequest>, MeteringPointMasterDataAccessValidation>();
        services.AddSingleton<IAccessValidator<MeasurementsAccessValidationRequest>, MeteringPointMeasurementDataAccessValidation>();

        services.AddSingleton<AuthorizationService>(provider =>
        {
            var tokenCredentials = new DefaultAzureCredential();
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            var accessValidatorDispatchService = provider.GetRequiredService<IAccessValidatorDispatchService>();
            var keyClient = new KeyClient(options.Value.AuthSignKeyVault, tokenCredentials);
            return new AuthorizationService(keyClient, options.Value.AuthSignKeyName, accessValidatorDispatchService);
        });

        services.AddHealthChecks()
          .AddElectricityMarketDataApiHealthCheck();
    }
}
