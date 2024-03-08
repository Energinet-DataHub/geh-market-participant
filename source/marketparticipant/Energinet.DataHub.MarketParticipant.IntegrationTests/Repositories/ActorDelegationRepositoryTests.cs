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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class ActorDelegationRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public ActorDelegationRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAsync_DelegationNotExists_ReturnsNull()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var contactRepository = new ActorDelegationRepository(context);

            // Act
            var testContact = await contactRepository
                .GetAsync(new ActorDelegationId(Guid.NewGuid()));

            // Assert
            Assert.Null(testContact);
        }

        [Fact]
        public async Task AddAsync_OneDelegationExpiresAtIsNull_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var actorFrom = await _fixture.PrepareActorAsync();
            var actorFromId = new ActorId(actorFrom.Id);
            var actorTo = await _fixture.PrepareActorAsync();
            var actorToId = new ActorId(actorTo.Id);
            var gridArea = await _fixture.PrepareGridAreaAsync();
            var gridAreaId = new GridAreaId(gridArea.Id);
            var actorDelegationRepository = new ActorDelegationRepository(context);
            var startsAt = DateTimeOffset.UtcNow.ToInstant();
            var testDelegation = new ActorDelegation(
                actorFromId,
                actorToId,
                gridAreaId,
                DelegationMessageType.RSM012Inbound,
                startsAt,
                null);

            // Act
            var delegationId = await actorDelegationRepository.AddOrUpdateAsync(testDelegation);
            var newDelegation = await actorDelegationRepository.GetAsync(delegationId);

            // Assert
            Assert.NotNull(newDelegation);
            Assert.NotEqual(Guid.Empty, newDelegation.Id.Value);
            Assert.Equal(actorFromId, newDelegation.DelegatedBy);
            Assert.Equal(actorToId, newDelegation.DelegatedTo);
            Assert.Equal(gridAreaId, newDelegation.GridAreaId);
            Assert.Equal(DelegationMessageType.RSM012Inbound, newDelegation.MessageType);
            Assert.Equal(startsAt, newDelegation.StartsAt);
            Assert.Null(newDelegation.ExpiresAt);
        }

        [Fact]
        public async Task AddAsync_OneDelegationExpiresAtIsNotNull_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var actorFrom = await _fixture.PrepareActorAsync();
            var actorFromId = new ActorId(actorFrom.Id);
            var actorTo = await _fixture.PrepareActorAsync();
            var actorToId = new ActorId(actorTo.Id);
            var gridArea = await _fixture.PrepareGridAreaAsync();
            var gridAreaId = new GridAreaId(gridArea.Id);
            var actorDelegationRepository = new ActorDelegationRepository(context);
            var startsAt = DateTimeOffset.UtcNow.ToInstant();
            var expiresAt = DateTimeOffset.UtcNow.AddDays(5).ToInstant();
            var testDelegation = new ActorDelegation(
                actorFromId,
                actorToId,
                gridAreaId,
                DelegationMessageType.RSM012Inbound,
                startsAt,
                expiresAt);

            // Act
            var delegationId = await actorDelegationRepository.AddOrUpdateAsync(testDelegation);
            var newDelegation = await actorDelegationRepository.GetAsync(delegationId);

            // Assert
            Assert.NotNull(newDelegation);
            Assert.NotEqual(Guid.Empty, newDelegation.Id.Value);
            Assert.Equal(actorFromId, newDelegation.DelegatedBy);
            Assert.Equal(actorToId, newDelegation.DelegatedTo);
            Assert.Equal(gridAreaId, newDelegation.GridAreaId);
            Assert.Equal(DelegationMessageType.RSM012Inbound, newDelegation.MessageType);
            Assert.Equal(startsAt, newDelegation.StartsAt);
            Assert.Equal(expiresAt, newDelegation.ExpiresAt);
        }

        [Fact]
        public async Task GetAsync_DifferentContexts_CanReadBack()
        {
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var contextReadback = _fixture.DatabaseManager.CreateDbContext();

            var actorDelegationRepository = new ActorDelegationRepository(context);
            var actorDelegationRepositoryReadback = new ActorDelegationRepository(contextReadback);

            var actorFrom = await _fixture.PrepareActorAsync();
            var actorFromId = new ActorId(actorFrom.Id);
            var actorTo = await _fixture.PrepareActorAsync();
            var actorToId = new ActorId(actorTo.Id);
            var gridArea = await _fixture.PrepareGridAreaAsync();
            var gridAreaId = new GridAreaId(gridArea.Id);
            var startsAt = DateTimeOffset.UtcNow.ToInstant();
            var expiresAt = DateTimeOffset.UtcNow.AddDays(5).ToInstant();
            var testDelegation = new ActorDelegation(
                actorFromId,
                actorToId,
                gridAreaId,
                DelegationMessageType.RSM012Inbound,
                startsAt,
                expiresAt);

            // Act
            var delegationId = await actorDelegationRepository.AddOrUpdateAsync(testDelegation);
            var newDelegation = await actorDelegationRepositoryReadback.GetAsync(delegationId);

            // Assert
            Assert.NotNull(newDelegation);
            Assert.NotEqual(Guid.Empty, newDelegation.Id.Value);
            Assert.Equal(actorFromId, newDelegation.DelegatedBy);
            Assert.Equal(actorToId, newDelegation.DelegatedTo);
            Assert.Equal(gridAreaId, newDelegation.GridAreaId);
            Assert.Equal(DelegationMessageType.RSM012Inbound, newDelegation.MessageType);
            Assert.Equal(startsAt, newDelegation.StartsAt);
            Assert.Equal(expiresAt, newDelegation.ExpiresAt);
        }
    }
}
