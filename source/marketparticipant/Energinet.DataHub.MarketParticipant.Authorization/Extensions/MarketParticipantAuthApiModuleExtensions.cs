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

using Energinet.DataHub.MarketParticipant.Authorization.Options;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.Authorization.Extensions;

internal static class MarketParticipantAuthApiModuleExtensions
{
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services)
    {
        services.AddOptions<KeyVaultOptions>().BindConfiguration(KeyVaultOptions.SectionName).ValidateDataAnnotations();

        services.AddSingleton<IVerifyAuthorization>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            return new AuthorizationService(options.Value.AuthSignKeyVault, options.Value.AuthSignKeyName, provider.GetRequiredService<ILogger<AuthorizationService>>());
        });

        services.AddSingleton<IRequestAuthorization>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            return new AuthorizationService(options.Value.AuthSignKeyVault, options.Value.AuthSignKeyName, provider.GetRequiredService<ILogger<AuthorizationService>>());
        });

        return services;
    }
}
