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
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions.DependencyInjection;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests;

public sealed class OrganizationIntegrationTestHost : IAsyncDisposable
{
    public IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    public static Task<OrganizationIntegrationTestHost> InitializeAsync(MarketParticipantDatabaseFixture databaseFixture)
    {
        ArgumentNullException.ThrowIfNull(databaseFixture);

        var configuration = BuildConfig(databaseFixture.DatabaseManager.ConnectionString);

        var host = new OrganizationIntegrationTestHost();
        host.ServiceCollection.AddSingleton(configuration);
        host.ServiceCollection.AddMarketParticipantOrganizationModule(configuration);
        InitEmailSender(host.ServiceCollection);

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
            new("Database:ConnectionString", dbConnectionString),

            new("AzureB2c:Tenant", "fake_value"),
            new("AzureB2c:SpnId", Guid.Empty.ToString()),
            new("AzureB2c:SpnSecret", Guid.NewGuid().ToString()),
            new("AzureB2c:BackendObjectId", Guid.Empty.ToString()),
            new("AzureB2c:BackendSpnObjectId", Guid.Empty.ToString()),
            new("AzureB2c:BackendId", Guid.Empty.ToString()),

            new("SendGrid:ApiKey", "fake_value"),
            new("SendGrid:SenderEmail", "fake_value"),
            new("SendGrid:BccEmail", "fake_value"),

            new("UserInvite:InviteFlowUrl", "https://fake_value"),

            new("Environment:Description", "fake_value"),

            new("KeyVault:CertificatesKeyVault", "https://fake_value"),

            new("CvrUpdate:NotificationToEmail", "fake_value@fake_value_test.dk"),
            new("BalanceResponsibleChanged:NotificationToEmail", "fake_value@fake_value_test.dk"),
            new($"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.TopicName)}", "fake_value"),
            new($"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.SubscriptionName)}", "fake_value"),
            new($"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}", "fake_value"),
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(keyValuePairs)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void InitEmailSender(IServiceCollection services)
    {
        var emailSender = new Mock<IEmailSender>();
        services.AddScoped(_ => emailSender.Object);
    }
}
