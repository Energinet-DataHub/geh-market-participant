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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.EntryPoint.LocalWebApi;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests;

public sealed class WebApiIntegrationTestHost : IAsyncDisposable
{
    private readonly Startup _startup;

    private WebApiIntegrationTestHost(IConfiguration configuration)
    {
        _startup = new NoAuthStartup(configuration);
    }

    public IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    public static Task<WebApiIntegrationTestHost> InitializeAsync(MarketParticipantDatabaseFixture databaseFixture, B2CFixture? b2CFixture = null, CertificateFixture? certificateFixture = null)
    {
        ArgumentNullException.ThrowIfNull(databaseFixture);

        var configuration = BuildConfig(databaseFixture.DatabaseManager.ConnectionString);

        var host = new WebApiIntegrationTestHost(configuration);
        host.ServiceCollection.AddSingleton(configuration);
        host._startup.ConfigureServices(host.ServiceCollection);
        InitUserIdProvider(host.ServiceCollection);

        if (b2CFixture != null)
        {
            host.ServiceCollection.Replace(ServiceDescriptor.Scoped<IActiveDirectoryB2CService>(_ => b2CFixture.B2CService));
        }

        if (certificateFixture != null)
        {
            host.ServiceCollection.Replace(ServiceDescriptor.Scoped<ICertificateService>(_ => certificateFixture.CertificateService));
        }

        return Task.FromResult(host);
    }

    public AsyncServiceScope BeginScope()
    {
        var serviceProvider = ServiceCollection.BuildServiceProvider();
        return serviceProvider.CreateAsyncScope();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private static IConfiguration BuildConfig(string dbConnectionString)
    {
        KeyValuePair<string, string?>[] keyValuePairs =
        {
            new(Settings.SqlDbConnectionString.Key, dbConnectionString),
            new(Settings.ExternalOpenIdUrl.Key, "fake_value"),
            new(Settings.BackendBffAppId.Key, "fake_value"),
            new(Settings.InternalOpenIdUrl.Key, "fake_value"),
            new(Settings.CertificateKeyVault.Key, "fake_value"),
            new(Settings.B2CBackendServicePrincipalNameObjectId.Key, "fake_value"),
            new(Settings.B2CBackendId.Key, "fake_value"),
            new(Settings.B2CBackendObjectId.Key, "fake_value"),
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(keyValuePairs)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void InitUserIdProvider(IServiceCollection services)
    {
        var mockUser = new FrontendUser(
            KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
            Guid.NewGuid(),
            Guid.NewGuid(),
            false);

        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        services.AddSingleton(userIdProvider.Object);
    }
}
