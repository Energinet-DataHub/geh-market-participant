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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;
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
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var balanceResponsibilityRequest = CreateBalanceResponsibilityRequest(
            new MockedGln(),
            new MockedGln(),
            new GridAreaCode(gridArea.Code));

        // Act
        await target.EnqueueAsync(balanceResponsibilityRequest);

        // Assert
        await context.BalanceResponsibilityRequests.SingleAsync(request =>
            request.EnergySupplier == balanceResponsibilityRequest.EnergySupplier.Value &&
            request.BalanceResponsibleParty == balanceResponsibilityRequest.BalanceResponsibleParty.Value &&
            request.GridAreaCode == balanceResponsibilityRequest.GridAreaCode.Value &&
            request.MeteringPointType == (int)balanceResponsibilityRequest.MeteringPointType &&
            request.ValidFrom == balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset() &&
            request.ValidTo == balanceResponsibilityRequest.ValidTo!.Value.ToDateTimeOffset());
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
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorC = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);

        var balanceResponsibilityRequestA = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), ActorNumber.Create(actorC.ActorNumber), new GridAreaCode(gridArea.Code));
        var balanceResponsibilityRequestB = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), ActorNumber.Create(actorC.ActorNumber), new GridAreaCode(gridArea.Code));
        balanceResponsibilityRequestB = balanceResponsibilityRequestB with
        {
            ValidFrom = balanceResponsibilityRequestB.ValidFrom.Plus(Duration.FromDays(40)),
            ValidTo = balanceResponsibilityRequestB.ValidTo?.Plus(Duration.FromDays(40))
        };

        var balanceResponsibilityRequestC = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorB.ActorNumber), ActorNumber.Create(actorC.ActorNumber), new GridAreaCode(gridArea.Code));
        balanceResponsibilityRequestC = balanceResponsibilityRequestC with
        {
            ValidFrom = balanceResponsibilityRequestC.ValidFrom.Plus(Duration.FromDays(40)),
            ValidTo = balanceResponsibilityRequestC.ValidTo?.Plus(Duration.FromDays(40))
        };

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.EnqueueAsync(balanceResponsibilityRequestB);
        await target.EnqueueAsync(balanceResponsibilityRequestC);

        // Act
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Assert
        context
            .BalanceResponsibilityRequests
            .Should()
            .NotContain(request => request.EnergySupplier == actorA.ActorNumber);

        context
            .BalanceResponsibilityRequests
            .Should()
            .ContainSingle(request => request.EnergySupplier == actorB.ActorNumber);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_HasBrpRequests_ProcessesRequest()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var expected = new MockedGln();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier, expected);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty, expected);

        var balanceResponsibilityRequest = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), ActorNumber.Create(actorB.ActorNumber), new GridAreaCode(gridArea.Code));
        await target.EnqueueAsync(balanceResponsibilityRequest);

        // Act
        await target.ProcessNextRequestsAsync(new ActorId(actorB.Id));

        // Assert
        context
            .BalanceResponsibilityRequests
            .Should()
            .NotContain(request => request.BalanceResponsibleParty == expected);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_UnrelatedActor_DoesNotProcess()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);
        var actorC = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);
        var actorD = await PrepareActorAsync(EicFunction.EnergySupplier);

        var balanceResponsibilityRequestA = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), ActorNumber.Create(actorC.ActorNumber), new GridAreaCode(gridArea.Code));
        var balanceResponsibilityRequestB = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorD.ActorNumber), ActorNumber.Create(actorB.ActorNumber), new GridAreaCode(gridArea.Code));

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.EnqueueAsync(balanceResponsibilityRequestB);

        // Act
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Assert
        context
            .BalanceResponsibilityRequests
            .Should()
            .ContainSingle(request => request.BalanceResponsibleParty == actorB.ActorNumber);

        context
            .BalanceResponsibilityRequests
            .Should()
            .NotContain(request => request.EnergySupplier == actorA.ActorNumber);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_UnrelatedMeteringPointType_DoesNotProcess()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);

        var balanceResponsibilityRequestA = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), ActorNumber.Create(actorB.ActorNumber), new GridAreaCode(gridArea.Code));
        var balanceResponsibilityRequestB = CreateBalanceResponsibilityRequest(ActorNumber.Create(actorA.ActorNumber), ActorNumber.Create(actorB.ActorNumber), new GridAreaCode(gridArea.Code));
        balanceResponsibilityRequestB = balanceResponsibilityRequestB with
        {
            MeteringPointType = MeteringPointType.E17Consumption
        };

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.EnqueueAsync(balanceResponsibilityRequestB);

        // Act
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Assert
        var actual = await context
            .BalanceResponsibilityAgreements
            .Where(request => request.EnergySupplierId == actorA.Id)
            .ToListAsync();

        Assert.Equal(2, actual.Count);
        Assert.Contains(actual, agreement => agreement.MeteringPointType == (int)MeteringPointType.E18Production);
        Assert.Contains(actual, agreement => agreement.MeteringPointType == (int)MeteringPointType.E17Consumption);

        context
            .BalanceResponsibilityRequests
            .Should()
            .NotContain(request => request.EnergySupplier == actorA.ActorNumber);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_SupportedOverlapIdentical_DoesNothing()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);

        var balanceResponsibilityRequest = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 7).ToDateTimeOffset().ToInstant(),
            null);

        await target.EnqueueAsync(balanceResponsibilityRequest);
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Act
        await target.EnqueueAsync(balanceResponsibilityRequest);
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Assert
        var actual = context
            .BalanceResponsibilityAgreements
            .Single(agreement =>
                agreement.EnergySupplierId == actorA.Id &&
                agreement.BalanceResponsiblePartyId == actorB.Id);

        Assert.Equal(balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset(), actual.ValidFrom);
        Assert.Equal(balanceResponsibilityRequest.ValidTo?.ToDateTimeOffset(), actual.ValidTo);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_SupportedOverlapEndDate_UpdatesEndDate()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);

        var balanceResponsibilityRequestA = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 7).ToDateTimeOffset().ToInstant(),
            null);

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Act
        var balanceResponsibilityRequestB = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 7).ToDateTimeOffset().ToInstant(),
            new DateTime(2024, 4, 17).ToDateTimeOffset().ToInstant());

        await target.EnqueueAsync(balanceResponsibilityRequestB);
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Assert
        var actual = context
            .BalanceResponsibilityAgreements
            .Single(agreement =>
                agreement.EnergySupplierId == actorA.Id &&
                agreement.BalanceResponsiblePartyId == actorB.Id);

        Assert.Equal(balanceResponsibilityRequestB.ValidFrom.ToDateTimeOffset(), actual.ValidFrom);
        Assert.Equal(balanceResponsibilityRequestB.ValidTo?.ToDateTimeOffset(), actual.ValidTo);
    }

    [Fact]
    public async Task ProcessNextRequestAsync_MultipleOverlaps_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);

        var balanceResponsibilityRequestA = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 7).ToDateTimeOffset().ToInstant(),
            new DateTime(2024, 4, 8).ToDateTimeOffset().ToInstant());

        var balanceResponsibilityRequestB = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 8).ToDateTimeOffset().ToInstant(),
            new DateTime(2024, 4, 9).ToDateTimeOffset().ToInstant());

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.EnqueueAsync(balanceResponsibilityRequestB);
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Act
        var balanceResponsibilityRequestC = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 3, 7).ToDateTimeOffset().ToInstant(),
            new DateTime(2024, 4, 17).ToDateTimeOffset().ToInstant());

        await target.EnqueueAsync(balanceResponsibilityRequestC);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.ProcessNextRequestsAsync(new ActorId(actorA.Id)));
    }

    [Theory]
    [InlineData("2024-04-17T00:00Z", true)]
    [InlineData("2024-04-16T00:00Z", false)]
    [InlineData("2024-04-18T00:00Z", false)]
    public async Task ProcessNextRequestAsync_EndDateOverlap_Validates(DateTimeOffset existingPeriodEnd, bool isValid)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        var actorA = await PrepareActorAsync(EicFunction.EnergySupplier);
        var actorB = await PrepareActorAsync(EicFunction.BalanceResponsibleParty);

        var balanceResponsibilityRequestA = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 7).ToDateTimeOffset().ToInstant(),
            existingPeriodEnd.ToInstant());

        await target.EnqueueAsync(balanceResponsibilityRequestA);
        await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));

        // Act
        var balanceResponsibilityRequestB = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E17Consumption,
            new DateTime(2024, 4, 7).ToDateTimeOffset().ToInstant(),
            new DateTime(2024, 4, 17).ToDateTimeOffset().ToInstant());

        await target.EnqueueAsync(balanceResponsibilityRequestB);

        // Assert
        if (isValid)
        {
            await target.ProcessNextRequestsAsync(new ActorId(actorA.Id));
        }
        else
        {
            await Assert.ThrowsAsync<ValidationException>(() => target.ProcessNextRequestsAsync(new ActorId(actorA.Id)));
        }
    }

    private static BalanceResponsibilityRequest CreateBalanceResponsibilityRequest(
        ActorNumber energySupplier,
        ActorNumber balanceResponsibleParty,
        GridAreaCode gridAreaCode)
    {
        return new BalanceResponsibilityRequest(
            energySupplier,
            balanceResponsibleParty,
            gridAreaCode,
            MeteringPointType.E18Production,
            new DateTime(2024, 1, 1).ToDateTimeOffset().ToInstant(),
            new DateTime(2024, 1, 31).ToDateTimeOffset().ToInstant());
    }

    private async Task<ActorEntity> PrepareActorAsync(EicFunction function, ActorNumber? actorNumber = null)
    {
        return await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(actor => actor.ActorNumber = actorNumber?.Value ?? new MockedGln().ToString()),
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = function));
    }
}
