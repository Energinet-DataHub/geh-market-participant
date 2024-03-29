﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ProcessDelegationRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly IEntityLock _lock;

    public ProcessDelegationRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
        _lock = new Mock<IEntityLock>().Object;
    }

    [Fact]
    public async Task GetForActorAsync_NoDelegation_ReturnsNull()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var processDelegationRepository = new ProcessDelegationRepository(context);

        // Act
        var actual = await processDelegationRepository
            .GetForActorAsync(new ActorId(Guid.NewGuid()), DelegatedProcess.RequestEnergyResults);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task GetForActorAsync_MultipleDelegations_GetsCorrectOne()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntityA = await _fixture.PrepareActiveActorAsync();
        var actorEntityB = await _fixture.PrepareActiveActorAsync();
        var actorEntityC = await _fixture.PrepareActiveActorAsync();
        var gridAreaId = await _fixture.PrepareGridAreaAsync();
        var actorRepository = new ActorRepository(context, _lock);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));
        var actorC = await actorRepository.GetAsync(new ActorId(actorEntityC.Id));

        var expectedA = new ProcessDelegation(actorA!, DelegatedProcess.RequestWholesaleResults);
        var expectedB = new ProcessDelegation(actorB!, DelegatedProcess.ReceiveEnergyResults);

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        expectedA.DelegateTo(actorC!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expectedB.DelegateTo(actorC.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var processDelegationRepository = new ProcessDelegationRepository(context);

        // Act
        await processDelegationRepository.AddOrUpdateAsync(expectedA);
        var processDelegationId = await processDelegationRepository.AddOrUpdateAsync(expectedB);

        // Assert
        var actual = await processDelegationRepository
            .GetForActorAsync(actorB!.Id, DelegatedProcess.ReceiveEnergyResults);

        Assert.NotNull(actual);
        Assert.Equal(processDelegationId, actual.Id);
        Assert.Equal(expectedB.DelegatedBy, actual.DelegatedBy);
        Assert.Equal(expectedB.Process, actual.Process);

        var actualDelegation = actual.Delegations.Single();

        Assert.Equal(actorC.Id, actualDelegation.DelegatedTo);
        Assert.Equal(new GridAreaId(gridAreaId.Id), actualDelegation.GridAreaId);
        Assert.Equal(baseDateTime, actualDelegation.StartsAt);
        Assert.Null(actualDelegation.StopsAt);
    }

    [Fact]
    public async Task GetForActorAsync_WithoutProcess_GetsAll()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntityA = await _fixture.PrepareActiveActorAsync();
        var actorEntityB = await _fixture.PrepareActiveActorAsync();
        var actorEntityC = await _fixture.PrepareActiveActorAsync();
        var gridAreaId = await _fixture.PrepareGridAreaAsync();
        var actorRepository = new ActorRepository(context, _lock);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));
        var actorC = await actorRepository.GetAsync(new ActorId(actorEntityC.Id));

        var expectedA = new ProcessDelegation(actorA!, DelegatedProcess.ReceiveEnergyResults);
        var expectedB = new ProcessDelegation(actorA!, DelegatedProcess.RequestWholesaleResults);

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        expectedA.DelegateTo(actorB!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expectedB.DelegateTo(actorC!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var processDelegationRepository = new ProcessDelegationRepository(context);

        // Act
        await processDelegationRepository.AddOrUpdateAsync(expectedA);
        await processDelegationRepository.AddOrUpdateAsync(expectedB);

        // Assert
        var actual = (await processDelegationRepository.GetForActorAsync(actorA!.Id)).ToList();

        Assert.NotNull(actual);
        Assert.Contains(actual, md => md.DelegatedBy == actorA.Id && md.Process == DelegatedProcess.ReceiveEnergyResults);
        Assert.Contains(actual, md => md.DelegatedBy == actorA.Id && md.Process == DelegatedProcess.RequestWholesaleResults);
    }

    [Fact]
    public async Task GetDelegatedToActorAsync_HasTwoDelegations_BothReturned()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntityA = await _fixture.PrepareActiveActorAsync();
        var actorEntityB = await _fixture.PrepareActiveActorAsync();
        var actorEntityC = await _fixture.PrepareActiveActorAsync();
        var gridAreaId = await _fixture.PrepareGridAreaAsync();
        var actorRepository = new ActorRepository(context, _lock);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));
        var actorC = await actorRepository.GetAsync(new ActorId(actorEntityC.Id));

        var expectedA = new ProcessDelegation(actorA!, DelegatedProcess.ReceiveEnergyResults);
        var expectedB = new ProcessDelegation(actorB!, DelegatedProcess.RequestWholesaleResults);

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        expectedA.DelegateTo(actorC!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expectedB.DelegateTo(actorC.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var processDelegationRepository = new ProcessDelegationRepository(context);
        await processDelegationRepository.AddOrUpdateAsync(expectedA);
        await processDelegationRepository.AddOrUpdateAsync(expectedB);

        // Act
        var actual = (await processDelegationRepository
            .GetDelegatedToActorAsync(actorC.Id))
            .ToList();

        // Assert
        Assert.NotNull(actual);
        Assert.Contains(actual, md => md.DelegatedBy == actorA!.Id && md.Process == DelegatedProcess.ReceiveEnergyResults);
        Assert.Contains(actual, md => md.DelegatedBy == actorB!.Id && md.Process == DelegatedProcess.RequestWholesaleResults);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ValidDelegation_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntityA = await _fixture.PrepareActiveActorAsync();
        var actorEntityB = await _fixture.PrepareActiveActorAsync();
        var gridAreaId = await _fixture.PrepareGridAreaAsync();
        var actorRepository = new ActorRepository(context, _lock);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);

        var expected = new ProcessDelegation(actorA!, DelegatedProcess.ReceiveWholesaleResults);
        expected.DelegateTo(actorB!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expected.StopDelegation(expected.Delegations.Last(), baseDateTime.Plus(Duration.FromDays(2)));
        expected.DelegateTo(actorB.Id, new GridAreaId(gridAreaId.Id), baseDateTime.Plus(Duration.FromDays(5)));

        var processDelegationRepository = new ProcessDelegationRepository(context);

        // Act
        var processDelegationId = await processDelegationRepository.AddOrUpdateAsync(expected);

        // Assert
        var actual = await processDelegationRepository
            .GetForActorAsync(actorA!.Id, DelegatedProcess.ReceiveWholesaleResults);

        Assert.NotNull(actual);
        Assert.Equal(processDelegationId, actual.Id);
        Assert.Equal(expected.DelegatedBy, actual.DelegatedBy);
        Assert.Equal(expected.Process, actual.Process);

        var actualDelegationA = actual.Delegations.First();

        Assert.Equal(actorB.Id, actualDelegationA.DelegatedTo);
        Assert.Equal(new GridAreaId(gridAreaId.Id), actualDelegationA.GridAreaId);
        Assert.Equal(baseDateTime, actualDelegationA.StartsAt);
        Assert.Equal(baseDateTime.Plus(Duration.FromDays(2)), actualDelegationA.StopsAt);

        var actualDelegationB = actual.Delegations.Last();

        Assert.Equal(actorB.Id, actualDelegationB.DelegatedTo);
        Assert.Equal(new GridAreaId(gridAreaId.Id), actualDelegationB.GridAreaId);
        Assert.Equal(baseDateTime.Plus(Duration.FromDays(5)), actualDelegationB.StartsAt);
        Assert.Null(actualDelegationB.StopsAt);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UpdatedStop_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntityA = await _fixture.PrepareActiveActorAsync();
        var actorEntityB = await _fixture.PrepareActiveActorAsync();
        var gridAreaId = await _fixture.PrepareGridAreaAsync();
        var actorRepository = new ActorRepository(context, _lock);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        var expectedStop = baseDateTime.Plus(Duration.FromDays(10));

        var expected = new ProcessDelegation(actorA!, DelegatedProcess.RequestEnergyResults);
        expected.DelegateTo(actorB!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var processDelegationRepository = new ProcessDelegationRepository(context);
        await processDelegationRepository.AddOrUpdateAsync(expected);

        var toUpdate = await processDelegationRepository.GetForActorAsync(actorA!.Id, DelegatedProcess.RequestEnergyResults);
        toUpdate!.StopDelegation(toUpdate.Delegations.Single(), expectedStop);

        // Act
        var processDelegationId = await processDelegationRepository.AddOrUpdateAsync(toUpdate);

        // Assert
        var actual = await processDelegationRepository
            .GetForActorAsync(actorA.Id, DelegatedProcess.RequestEnergyResults);

        Assert.NotNull(actual);
        Assert.Equal(processDelegationId, actual.Id);
        Assert.Equal(expected.DelegatedBy, actual.DelegatedBy);
        Assert.Equal(expected.Process, actual.Process);

        var actualDelegation = actual.Delegations.Single();

        Assert.Equal(actorB.Id, actualDelegation.DelegatedTo);
        Assert.Equal(new GridAreaId(gridAreaId.Id), actualDelegation.GridAreaId);
        Assert.Equal(baseDateTime, actualDelegation.StartsAt);
        Assert.Equal(expectedStop, actualDelegation.StopsAt);
    }
}
