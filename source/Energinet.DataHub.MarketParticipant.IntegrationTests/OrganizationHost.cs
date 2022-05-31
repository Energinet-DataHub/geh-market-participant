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
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests
{
    public sealed class OrganizationHost : IAsyncDisposable
    {
        private readonly Startup _startup;

        private OrganizationHost()
        {
            _startup = new Startup();
        }

        public static Task<OrganizationHost> InitializeAsync()
        {
            var host = new OrganizationHost();

            var configuration = BuildConfig();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(configuration);
            host._startup.Initialize(configuration, serviceCollection);
            serviceCollection
                .BuildServiceProvider()
                .UseSimpleInjector(host._startup.Container, o => o.Container.Options.EnableAutoVerification = false);

            host._startup.Container.Options.AllowOverridingRegistrations = true;
            return Task.FromResult(host);
        }

        public Scope BeginScope()
        {
            return AsyncScopedLifestyle.BeginScope(_startup.Container);
        }

        public async ValueTask DisposeAsync()
        {
            await _startup.DisposeAsync().ConfigureAwait(false);
        }

        private static IConfiguration BuildConfig()
        {
            KeyValuePair<string, string>[] keyValuePairs =
            {
                new(Settings.SqlDbConnectionString.Key, "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Database=marketparticipant;Connection Timeout=3"),
                new(Settings.ServiceBusHealthCheckConnectionString.Key, "fake_value"),
                new(Settings.ServiceBusTopicName.Key, "fake_value")
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(keyValuePairs)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
