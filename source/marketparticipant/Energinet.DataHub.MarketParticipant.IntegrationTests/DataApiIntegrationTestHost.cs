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
using Energinet.DataHub.MarketParticipant.EntryPoint.DataApi.Extensions.DependencyInjection;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests;

public sealed class DataApiIntegrationTestHost : IAsyncDisposable
{
    public IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    public static Task<DataApiIntegrationTestHost> InitializeAsync(MarketParticipantDatabaseFixture databaseFixture)
    {
        ArgumentNullException.ThrowIfNull(databaseFixture);

        var configuration = BuildConfig(databaseFixture.DatabaseManager.ConnectionString);

        var host = new DataApiIntegrationTestHost();
        host.ServiceCollection.AddSingleton(configuration);
        host.ServiceCollection.AddMarketParticipantDataApiModule(configuration);

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
        [
            new("Database:ConnectionString", dbConnectionString),
        ];

        return new ConfigurationBuilder()
            .AddInMemoryCollection(keyValuePairs)
            .AddEnvironmentVariables()
            .Build();
    }
}
