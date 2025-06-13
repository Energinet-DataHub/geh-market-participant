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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Query;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class AdditionalRecipientQueryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public AdditionalRecipientQueryRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddOrUpdateAsync_NoRecipients_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = new AdditionalRecipientQueryRepository(context);

        // Act
        var actual = await target.GetAsync(MockedMeteringPointIdentifier.New());

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task AddOrUpdateAsync_HasRecipients_ReturnsRecipients()
    {
        // Arrange
        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var readContext = _fixture.DatabaseManager.CreateDbContext();

        var actorA = await _fixture.PrepareActorAsync();
        var actorB = await _fixture.PrepareActorAsync();
        var actorC = await _fixture.PrepareActorAsync();
        var actorD = await _fixture.PrepareActorAsync();

        var expectedMeteringPoint = MockedMeteringPointIdentifier.New();
        var unexpectedMeteringPoint = MockedMeteringPointIdentifier.New();

        await using var writeContext = _fixture.DatabaseManager.CreateDbContext();
        var writeRepository = new AdditionalRecipientRepository(writeContext);

        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorA.Id)) { OfMeteringPoints = { expectedMeteringPoint } });
        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorB.Id)) { OfMeteringPoints = { expectedMeteringPoint } });
        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorC.Id)) { OfMeteringPoints = { unexpectedMeteringPoint } });
        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorD.Id)) { OfMeteringPoints = { expectedMeteringPoint } });

        var target = new AdditionalRecipientQueryRepository(readContext);

        // Act
        var actual = await target.GetAsync(expectedMeteringPoint);

        // Assert
        var expectedAdditionalRecipients = new[] { actorA, actorB, actorD }
            .Select(a => (ActorNumber.Create(a.ActorNumber), a.MarketRole.Function))
            .ToHashSet();

        Assert.Equal(expectedAdditionalRecipients, actual.ToHashSet());
    }

    [Fact]
    public async Task AddOrUpdateAsync_HasInactiveRecipients_SkipsRecipients()
    {
        // Arrange
        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var readContext = _fixture.DatabaseManager.CreateDbContext();

        var actorA = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.New),
            TestPreparationEntities.ValidMarketRole);

        var actorB = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Inactive),
            TestPreparationEntities.ValidMarketRole);

        var actorC = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Passive),
            TestPreparationEntities.ValidMarketRole);

        var expectedMeteringPoint = MockedMeteringPointIdentifier.New();

        await using var writeContext = _fixture.DatabaseManager.CreateDbContext();
        var writeRepository = new AdditionalRecipientRepository(writeContext);

        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorA.Id)) { OfMeteringPoints = { expectedMeteringPoint } });
        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorB.Id)) { OfMeteringPoints = { expectedMeteringPoint } });
        await writeRepository.AddOrUpdateAsync(new AdditionalRecipient(new ActorId(actorC.Id)) { OfMeteringPoints = { expectedMeteringPoint } });

        var target = new AdditionalRecipientQueryRepository(readContext);

        // Act
        var actual = await target.GetAsync(expectedMeteringPoint);

        // Assert
        var expectedAdditionalRecipients = new[] { actorA, actorC }
            .Select(a => (ActorNumber.Create(a.ActorNumber), a.MarketRole.Function))
            .ToHashSet();

        Assert.Equal(expectedAdditionalRecipients, actual.ToHashSet());
    }
}
