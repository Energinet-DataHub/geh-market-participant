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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Configuration;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests
{
    public sealed class OrganizationIntegrationTestHost : IAsyncDisposable
    {
        private readonly Startup _startup;

        private OrganizationIntegrationTestHost()
        {
            _startup = new Startup();
        }

        public IServiceCollection ServiceCollection { get; } = new ServiceCollection();

        public static Task<OrganizationIntegrationTestHost> InitializeAsync(MarketParticipantDatabaseFixture databaseFixture)
        {
            ArgumentNullException.ThrowIfNull(databaseFixture);

            var configuration = BuildConfig(databaseFixture.DatabaseManager.ConnectionString);

            var host = new OrganizationIntegrationTestHost();
            host.ServiceCollection.AddSingleton(configuration);
            host._startup.Initialize(configuration, host.ServiceCollection);
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
                new(Settings.SqlDbConnectionString.Key, dbConnectionString),
                new(Settings.B2CBackendServicePrincipalNameObjectId.Key, Guid.Empty.ToString()),
                new(Settings.B2CBackendId.Key, Guid.Empty.ToString()),
                new(Settings.B2CBackendObjectId.Key, Guid.Empty.ToString()),
                new(Settings.SendGridApiKey.Key, "fake_value"),
                new(Settings.SenderEmail.Key, "fake_value"),
                new(Settings.BccEmail.Key, "fake_value"),
                new(Settings.OrganizationIdentityUpdateNotificationToEmail.Key, "fake_value@fake_value_test.dk"),
                new(Settings.BalanceResponsiblePartiesChangedNotificationToEmail.Key, "fake_value@fake_value_test.dk"),
                new(Settings.UserInviteFlow.Key, "fake_value"),
                new(Settings.EnvironmentDescription.Key, "fake_value"),
                new(Settings.ServiceBusTopicConnectionString.Key, "fake_value"),
                new(Settings.ServiceBusTopicName.Key, "fake_value"),
                new($"{nameof(ConsumeServiceBusSettings)}:{nameof(ConsumeServiceBusSettings.ConnectionString)}", "fake_value"),
                new($"{nameof(ConsumeServiceBusSettings)}:{nameof(ConsumeServiceBusSettings.SharedIntegrationEventTopic)}", "fake_value"),
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
}
