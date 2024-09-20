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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

internal sealed class RepositoryTarget<T> : IAsyncDisposable
{
    private readonly WebApiIntegrationTestHost _host;
    private readonly AsyncServiceScope _scope;
    private readonly MarketParticipantDbContext _context;

    private RepositoryTarget(WebApiIntegrationTestHost host, AsyncServiceScope scope, MarketParticipantDbContext context, T value)
    {
        _host = host;
        _scope = scope;
        _context = context;

        Value = value;
    }

    public T Value { get; }

    public static async Task<RepositoryTarget<T>> CreateAsync(MarketParticipantDatabaseFixture fixture, Func<IMarketParticipantDbContext, T> factory)
    {
        var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        var scope = host.BeginScope();
        var context = fixture.DatabaseManager.CreateDbContext();
        return new RepositoryTarget<T>(host, scope, context, factory(context));
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        await _scope.DisposeAsync();
        await _context.DisposeAsync();

        if (Value is IAsyncDisposable disposableValue)
        {
            await disposableValue.DisposeAsync().ConfigureAwait(false);
        }
    }
}
