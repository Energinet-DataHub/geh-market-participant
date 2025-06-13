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
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class AdditionalRecipientRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public AdditionalRecipientRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddOrUpdateAsync_NoMeteringPoints_CanBeReadBack()
    {
        // Arrange
        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actor = await _fixture.PrepareActorAsync();
        var actorId = new ActorId(actor.Id);

        var target = new AdditionalRecipientRepository(context);

        // Act
        var additionalRecipient = new AdditionalRecipient(actorId);
        await target.AddOrUpdateAsync(additionalRecipient);

        // Assert
        await using var readContext = _fixture.DatabaseManager.CreateDbContext();
        var repository = new AdditionalRecipientRepository(readContext);
        var actual = await repository.GetAsync(actorId);

        Assert.NotNull(actual);
        Assert.Equal(actorId, actual.Actor);
        Assert.Empty(actual.OfMeteringPoints);
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneMeteringPoint_CanBeReadBack()
    {
        // Arrange
        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actor = await _fixture.PrepareActorAsync();
        var actorId = new ActorId(actor.Id);

        var target = new AdditionalRecipientRepository(context);

        // Act
        var additionalRecipient = new AdditionalRecipient(actorId);
        var mockedMeteringPointId = MockedMeteringPointIdentifier.New();
        additionalRecipient.OfMeteringPoints.Add(mockedMeteringPointId);

        await target.AddOrUpdateAsync(additionalRecipient);

        // Assert
        await using var readContext = _fixture.DatabaseManager.CreateDbContext();
        var repository = new AdditionalRecipientRepository(readContext);
        var actual = await repository.GetAsync(actorId);

        Assert.NotNull(actual);
        Assert.Equal(actorId, actual.Actor);
        Assert.Single(actual.OfMeteringPoints, mp => mp == mockedMeteringPointId);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ChangedMeteringPoints_ChangesAreAddedRemoved()
    {
        // Arrange
        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        MeteringPointIdentification[] initialList =
        [
            MockedMeteringPointIdentifier.New(),
            MockedMeteringPointIdentifier.New(),
            MockedMeteringPointIdentifier.New(),
            MockedMeteringPointIdentifier.New(),
            MockedMeteringPointIdentifier.New()
        ];

        MeteringPointIdentification[] patchedList =
        [
            MockedMeteringPointIdentifier.New(),
            initialList[0],
            MockedMeteringPointIdentifier.New(),
            initialList[1],
            MockedMeteringPointIdentifier.New()
        ];

        var actor = await _fixture.PrepareActorAsync();
        var actorId = new ActorId(actor.Id);

        var target = new AdditionalRecipientRepository(context);

        // Setup initial list.
        {
            var initialAdditionalRecipient = new AdditionalRecipient(actorId);

            foreach (var mp in initialList)
            {
                initialAdditionalRecipient.OfMeteringPoints.Add(mp);
            }

            await target.AddOrUpdateAsync(initialAdditionalRecipient);
        }

        // Act
        var additionalRecipient = await target.GetAsync(actorId);
        Assert.NotNull(additionalRecipient);

        additionalRecipient.OfMeteringPoints.Clear();

        foreach (var mp in patchedList)
        {
            additionalRecipient.OfMeteringPoints.Add(mp);
        }

        await target.AddOrUpdateAsync(additionalRecipient);

        // Assert
        await using var readContext = _fixture.DatabaseManager.CreateDbContext();
        var repository = new AdditionalRecipientRepository(readContext);
        var actual = await repository.GetAsync(actorId);

        Assert.NotNull(actual);
        Assert.Equal(actorId, actual.Actor);
        Assert.Equal(patchedList.ToHashSet(), actual.OfMeteringPoints.ToHashSet());
    }
}
