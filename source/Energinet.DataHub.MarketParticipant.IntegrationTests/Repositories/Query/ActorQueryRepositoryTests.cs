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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Query;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class ActorQueryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ActorQueryRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetSelectionActorsAsync_ReturnsActors()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorId = await _fixture.DatabaseManager.CreateActorAsync(new[] { EicFunction.Agent });

        var target = new ActorQueryRepository(context);

        // act
        var actual = (await target.GetSelectionActorsAsync(new[] { actorId })).ToList();

        // assert
        Assert.NotEmpty(actual);
        Assert.NotNull(actual.First(x => x.Id == actorId));
    }
}
