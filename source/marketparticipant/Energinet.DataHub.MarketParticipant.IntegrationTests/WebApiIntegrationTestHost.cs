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
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common.Options;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests;

public sealed class WebApiIntegrationTestHost : IAsyncDisposable
{
    public IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    public static Task<WebApiIntegrationTestHost> InitializeAsync(MarketParticipantDatabaseFixture databaseFixture, B2CFixture? b2CFixture = null, CertificateFixture? certificateFixture = null)
    {
        ArgumentNullException.ThrowIfNull(databaseFixture);

        var configuration = BuildConfig(databaseFixture.DatabaseManager.ConnectionString);

        var host = new WebApiIntegrationTestHost();
        host.ServiceCollection.AddSingleton(configuration);
        host.ServiceCollection.AddMarketParticipantWebApiModule(configuration);
        InitUserIdProvider(host.ServiceCollection);

        if (b2CFixture != null)
        {
            host.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => b2CFixture.B2CService));
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

    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = BeginScope();
        return await action(scope.ServiceProvider);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private static IConfiguration BuildConfig(string dbConnectionString)
    {
        KeyValuePair<string, string?>[] keyValuePairs =
        {
            new("Database:ConnectionString", dbConnectionString),
            new($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.MitIdExternalMetadataAddress)}", "fake_value"),
            new($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.ExternalMetadataAddress)}", "fake_value"),
            new($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.InternalMetadataAddress)}", "fake_value"),
            new($"{nameof(UserAuthentication)}:{nameof(UserAuthentication.BackendBffAppId)}", "fake_value"),
            new("KeyVault:TokenSignKeyVault", "https://fake_value"),
            new("KeyVault:TokenSignKeyName", "fake_value"),
            new("KeyVault:CertificatesKeyVault", "https://fake_value"),
            new("AzureB2c:Tenant", "fake_value"),
            new("AzureB2c:SpnId", "fake_value"),
            new("AzureB2c:SpnSecret", "fake_value"),
            new("AzureB2c:BackendObjectId", "fake_value"),
            new("AzureB2c:BackendSpnObjectId", "fake_value"),
            new("AzureB2c:BackendId", "fake_value"),
            new($"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.TopicName)}", "fake_value"),
            new($"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.SubscriptionName)}", "fake_value"),
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
