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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class BalanceResponsibilityRequestRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public BalanceResponsibilityRequestRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EnqueueAsync_GivenRequest_AddsRequest()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();

        var balanceResponsibilityRequest = CreateBalanceResponsibilityRequest(
            new MockedGln(),
            new MockedGln());

        // Act
        await target.EnqueueAsync(balanceResponsibilityRequest);

        // Assert
        await context.BalanceResponsibilityRequests.SingleAsync(request =>
            request.EnergySupplier == balanceResponsibilityRequest.EnergySupplier.Value &&
            request.BalanceResponsibleParty == balanceResponsibilityRequest.BalanceResponsibleParty.Value &&
            request.GridAreaCode == balanceResponsibilityRequest.GridAreaCode.Value &&
            request.MeteringPointType == (int)balanceResponsibilityRequest.MeteringPointType &&
            request.ValidFrom == balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset() &&
            request.ValidTo == null);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_NoRequests_DoesNothing()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var actor = await _fixture.PrepareActorAsync();

        // Act + Assert
        await target.ProcessNextRequestsAsync(new ActorId(actor.Id));
    }

    [Fact]
    public async Task ProcessNextRequestAsync_HasRequests_ProcessesRequest()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var actorA = await _fixture.PrepareActorAsync();
        var actorB = await _fixture.PrepareActorAsync();

        var balanceResponsibilityRequestA = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), new MockedGln());
        var balanceResponsibilityRequestB = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), new MockedGln());
        var balanceResponsibilityRequestC = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorB.ActorNumber), new MockedGln());

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.EnqueueAsync(balanceResponsibilityRequestB);
        await target.EnqueueAsync(balanceResponsibilityRequestC);

        // Act
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Assert
    }

    [Fact]
    public async Task ProcessNextRequestAsync_HasBrpRequests_ProcessesRequest()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var actor = await _fixture.PrepareActorAsync();

        var balanceResponsibilityRequestA = CreateBalanceResponsibilityRequest(ActorNumber.Create(actor.ActorNumber), new MockedGln());
        var balanceResponsibilityRequestB = CreateBalanceResponsibilityRequest(new MockedGln(), ActorNumber.Create(actor.ActorNumber));

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.EnqueueAsync(balanceResponsibilityRequestB);

        // Act
        await target.ProcessNextRequestsAsync(new ActorId(actor.Id));
        await target.ProcessNextRequestsAsync(new ActorId(actor.Id));
        await target.ProcessNextRequestsAsync(new ActorId(actor.Id));

        // Assert
    }

    private static BalanceResponsibilityRequest CreateBalanceResponsibilityRequest(
        ActorNumber energySupplier,
        ActorNumber balanceResponsibleParty)
    {
        return new BalanceResponsibilityRequest(
            energySupplier,
            balanceResponsibleParty,
            new GridAreaCode("123"),
            MeteringPointType.E17Consumption,
            SystemClock.Instance.GetCurrentInstant(),
            null);
    }
}
