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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class InitiateMitIdSignupHandlerTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public InitiateMitIdSignupHandlerTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_UserFound_InitiatesMitIdSignup()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var user = await _fixture.PrepareUserAsync();

        var mediator = scope.GetInstance<IMediator>();
        var command = new InitiateMitIdSignupCommand(user.Id);

        // act
        await mediator.Send(command);
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actual = context.Users.First(x => x.Id == user.Id);

        // assert
        Assert.True(actual.MitIdSignupInitiatedTimestampUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}
