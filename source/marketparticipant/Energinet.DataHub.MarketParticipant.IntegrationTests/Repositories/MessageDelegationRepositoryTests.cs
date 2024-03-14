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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class MessageDelegationRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public MessageDelegationRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetForActorAsync_NoDelegation_ReturnsNull()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var messageDelegationRepository = new MessageDelegationRepository(context);

        // Act
        var actual = await messageDelegationRepository
            .GetForActorAsync(new ActorId(Guid.NewGuid()), DelegationMessageType.Rsm012Inbound);

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
        var actorRepository = new ActorRepository(context);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));
        var actorC = await actorRepository.GetAsync(new ActorId(actorEntityC.Id));

        var expectedA = new MessageDelegation(actorA!, DelegationMessageType.Rsm017Inbound);
        var expectedB = new MessageDelegation(actorB!, DelegationMessageType.Rsm016Outbound);

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        expectedA.DelegateTo(actorC!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expectedB.DelegateTo(actorC.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var messageDelegationRepository = new MessageDelegationRepository(context);

        // Act
        await messageDelegationRepository.AddOrUpdateAsync(expectedA);
        var messageId = await messageDelegationRepository.AddOrUpdateAsync(expectedB);

        // Assert
        var actual = await messageDelegationRepository
            .GetForActorAsync(actorB!.Id, DelegationMessageType.Rsm016Outbound);

        Assert.NotNull(actual);
        Assert.Equal(messageId, actual.Id);
        Assert.Equal(expectedB.DelegatedBy, actual.DelegatedBy);
        Assert.Equal(expectedB.MessageType, actual.MessageType);

        var actualDelegation = actual.Delegations.Single();

        Assert.Equal(actorC.Id, actualDelegation.DelegatedTo);
        Assert.Equal(new GridAreaId(gridAreaId.Id), actualDelegation.GridAreaId);
        Assert.Equal(baseDateTime, actualDelegation.StartsAt);
        Assert.Null(actualDelegation.StopsAt);
    }

    [Fact]
    public async Task GetForActorAsync_WithoutMessageType_GetsAll()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntityA = await _fixture.PrepareActiveActorAsync();
        var actorEntityB = await _fixture.PrepareActiveActorAsync();
        var actorEntityC = await _fixture.PrepareActiveActorAsync();
        var gridAreaId = await _fixture.PrepareGridAreaAsync();
        var actorRepository = new ActorRepository(context);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));
        var actorC = await actorRepository.GetAsync(new ActorId(actorEntityC.Id));

        var expectedA = new MessageDelegation(actorA!, DelegationMessageType.Rsm017Inbound);
        var expectedB = new MessageDelegation(actorA!, DelegationMessageType.Rsm016Outbound);

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        expectedA.DelegateTo(actorB!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expectedB.DelegateTo(actorC!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var messageDelegationRepository = new MessageDelegationRepository(context);

        // Act
        await messageDelegationRepository.AddOrUpdateAsync(expectedA);
        await messageDelegationRepository.AddOrUpdateAsync(expectedB);

        // Assert
        var actual = (await messageDelegationRepository.GetForActorAsync(actorA!.Id)).ToList();

        Assert.NotNull(actual);
        Assert.Contains(actual, md => md.DelegatedBy == actorA.Id && md.MessageType == DelegationMessageType.Rsm017Inbound);
        Assert.Contains(actual, md => md.DelegatedBy == actorA.Id && md.MessageType == DelegationMessageType.Rsm016Outbound);
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
        var actorRepository = new ActorRepository(context);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));
        var actorC = await actorRepository.GetAsync(new ActorId(actorEntityC.Id));

        var expectedA = new MessageDelegation(actorA!, DelegationMessageType.Rsm017Inbound);
        var expectedB = new MessageDelegation(actorB!, DelegationMessageType.Rsm016Outbound);

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        expectedA.DelegateTo(actorC!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expectedB.DelegateTo(actorC.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var messageDelegationRepository = new MessageDelegationRepository(context);
        await messageDelegationRepository.AddOrUpdateAsync(expectedA);
        await messageDelegationRepository.AddOrUpdateAsync(expectedB);

        // Act
        var actual = (await messageDelegationRepository
            .GetDelegatedToActorAsync(actorC.Id))
            .ToList();

        // Assert
        Assert.NotNull(actual);
        Assert.Contains(actual, md => md.DelegatedBy == actorA!.Id && md.MessageType == DelegationMessageType.Rsm017Inbound);
        Assert.Contains(actual, md => md.DelegatedBy == actorB!.Id && md.MessageType == DelegationMessageType.Rsm016Outbound);
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
        var actorRepository = new ActorRepository(context);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);

        var expected = new MessageDelegation(actorA!, DelegationMessageType.Rsm017Inbound);
        expected.DelegateTo(actorB!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);
        expected.StopDelegation(expected.Delegations.Last(), baseDateTime.Plus(Duration.FromDays(2)));
        expected.DelegateTo(actorB.Id, new GridAreaId(gridAreaId.Id), baseDateTime.Plus(Duration.FromDays(5)));

        var messageDelegationRepository = new MessageDelegationRepository(context);

        // Act
        var messageId = await messageDelegationRepository.AddOrUpdateAsync(expected);

        // Assert
        var actual = await messageDelegationRepository
            .GetForActorAsync(actorA!.Id, DelegationMessageType.Rsm017Inbound);

        Assert.NotNull(actual);
        Assert.Equal(messageId, actual.Id);
        Assert.Equal(expected.DelegatedBy, actual.DelegatedBy);
        Assert.Equal(expected.MessageType, actual.MessageType);

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
        var actorRepository = new ActorRepository(context);
        var actorA = await actorRepository.GetAsync(new ActorId(actorEntityA.Id));
        var actorB = await actorRepository.GetAsync(new ActorId(actorEntityB.Id));

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
        var expectedStop = baseDateTime.Plus(Duration.FromDays(10));

        var expected = new MessageDelegation(actorA!, DelegationMessageType.Rsm012Outbound);
        expected.DelegateTo(actorB!.Id, new GridAreaId(gridAreaId.Id), baseDateTime);

        var messageDelegationRepository = new MessageDelegationRepository(context);
        await messageDelegationRepository.AddOrUpdateAsync(expected);

        var toUpdate = await messageDelegationRepository.GetForActorAsync(actorA!.Id, DelegationMessageType.Rsm012Outbound);
        toUpdate!.StopDelegation(toUpdate.Delegations.Single(), expectedStop);

        // Act
        var messageId = await messageDelegationRepository.AddOrUpdateAsync(toUpdate);

        // Assert
        var actual = await messageDelegationRepository
            .GetForActorAsync(actorA.Id, DelegationMessageType.Rsm012Outbound);

        Assert.NotNull(actual);
        Assert.Equal(messageId, actual.Id);
        Assert.Equal(expected.DelegatedBy, actual.DelegatedBy);
        Assert.Equal(expected.MessageType, actual.MessageType);

        var actualDelegation = actual.Delegations.Single();

        Assert.Equal(actorB.Id, actualDelegation.DelegatedTo);
        Assert.Equal(new GridAreaId(gridAreaId.Id), actualDelegation.GridAreaId);
        Assert.Equal(baseDateTime, actualDelegation.StartsAt);
        Assert.Equal(expectedStop, actualDelegation.StopsAt);
    }
}
