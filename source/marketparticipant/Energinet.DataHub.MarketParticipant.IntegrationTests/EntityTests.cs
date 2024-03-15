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

using System.Diagnostics;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class EntityTests(MarketParticipantDatabaseFixture fixture, ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task CreateLockScope_IfLockAlreadyTaken_Waits()
    {
        // arrange
        var sw = Stopwatch.StartNew();

        // act
        var lockTimout1Async = Task.Run(() => LockTimoutAsync(1_500));
        var lockTimout2Async = Task.Run(() => LockTimoutAsync(1_500));

        await Task.WhenAll(lockTimout1Async, lockTimout2Async);

        sw.Stop();

        var elapsed = sw.ElapsedMilliseconds;
        var elapsedTicks = sw.ElapsedTicks;

        // assert
        var output = $"actual elapsed: {elapsed} | ticks: {elapsedTicks} | isHighRes: {Stopwatch.IsHighResolution} | freq: {Stopwatch.Frequency}";
        Assert.True(elapsed >= 3_000, output);
        testOutputHelper.WriteLine(output);
    }

    private async Task LockTimoutAsync(int millis)
    {
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        await using var context = fixture.DatabaseManager.CreateDbContext();

        var uowProvider = new UnitOfWorkProvider(context);

        await using var uow = await uowProvider.NewUnitOfWorkAsync();

        var entityLock = new EntityLock(context);

        var org = await fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(org.Id), new MockedGln(), new ActorName("test"));
        await entityLock.LockAsync(actor);

        await Task.Delay(millis);
    }
}
