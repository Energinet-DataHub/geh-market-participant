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

using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Authorization.Options;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.Authorization.Extensions;

internal static class MarketParticipantAuthApiModuleExtensions
{
    public static IServiceCollection AddAuthorizationRequestModule(this IServiceCollection services)
    {
        services.AddOptions<AuthorizationRequestOptions>().BindConfiguration(AuthorizationRequestOptions.SectionName).ValidateDataAnnotations();

        services.AddHttpClient("AuthorizationRequestClient", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<AuthorizationRequestOptions>>();
            client.BaseAddress = options.Value.EndpointUrl;
        });

        services.AddSingleton<IRequestAuthorization>(provider => new AuthorizationRequestService(provider.GetRequiredService<IHttpClientFactory>().CreateClient("AuthorizationRequestClient")));

        return services;
    }

    public static IServiceCollection AddAuthorizationVerifyModule(this IServiceCollection services)
    {
        services.AddOptions<AuthorizationVerifyOptions>().BindConfiguration(AuthorizationVerifyOptions.SectionName).ValidateDataAnnotations();

        services.AddSingleton<IVerifyAuthorization>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AuthorizationVerifyOptions>>();
            return new AuthorizationVerifyService(
                options.Value.AuthSignKeyVault,
                options.Value.AuthSignKeyName,
                provider.GetRequiredService<ILogger<AuthorizationVerifyService>>());
        });

        return services;
    }

    public static void AddDbContexts(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName).ValidateDataAnnotations();

        services.AddDbContext<IAuthorizationDbContext, AuthorizationDbContext>((provider, options) =>
        {
            var databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>();
            options.UseSqlServer(databaseOptions.Value.ConnectionString);
        });
    }
}
