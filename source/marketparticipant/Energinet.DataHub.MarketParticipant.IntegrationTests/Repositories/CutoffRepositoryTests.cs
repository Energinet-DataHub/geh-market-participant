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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CutoffRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public CutoffRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetCutoff_NeverSet_ReturnsUnixEpoch()
    {
        // arrange
        await using var target = await CreateTarget();

        // act
        var result = await target.Value.GetCutoffAsync((CutoffType)256);

        // assert
        Assert.Equal(Instant.FromUnixTimeTicks(0), result);
    }

    [Fact]
    public async Task GetCutoff_PreviouslySet_ReturnsSetCutoff()
    {
        // arrange
        await using var target = await CreateTarget();

        var expected = DateTimeOffset.Now.ToInstant();
        await target.Value.UpdateCutoffAsync((CutoffType)512, expected);

        // act
        var result = await target.Value.GetCutoffAsync((CutoffType)512);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpdateCutoff_AlreadyBeingUpdated_HandlesConcurrency()
    {
        // arrange
        await using var targetOne = await CreateTarget();
        await using var targetTwo = await CreateTarget();

        _ = await targetOne.Value.GetCutoffAsync((CutoffType)1024);
        _ = await targetTwo.Value.GetCutoffAsync((CutoffType)1024);

        // act, assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await targetOne.Value.UpdateCutoffAsync((CutoffType)1024, DateTime.UtcNow.ToInstant().Plus(Duration.FromMinutes(1)));
            await targetTwo.Value.UpdateCutoffAsync((CutoffType)1024, DateTime.UtcNow.ToInstant().Plus(Duration.FromMinutes(2)));
        });
    }

    private async Task<RepositoryTarget<ICutoffRepository>> CreateTarget() =>
        await RepositoryTarget<ICutoffRepository>.CreateAsync(_fixture, c => new CutoffRepository(c));
}
