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
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ClockUserLoginHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ClockUserLoginHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ClockUserLogin_TimestampProvided_SetsLatestLoginAt()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var user = await _fixture.PrepareUserAsync();

        // truncate to seconds
        var expected = Instant.FromUnixTimeSeconds(SystemClock.Instance.GetCurrentInstant().ToUnixTimeSeconds());

        // act
        await mediator.Send(new ClockUserLoginCommand(user.Id, expected));

        // assert
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actual = context.Users.Single(u => u.Id == user.Id).LatestLoginAt.GetValueOrDefault().ToInstant();

        Assert.Equal(expected, actual);
    }
}
