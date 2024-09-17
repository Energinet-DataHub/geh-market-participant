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
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class InitiateMitIdSignupHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public InitiateMitIdSignupHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_UserFound_InitiatesMitIdSignup()
    {
        var frontendUser = await _fixture.PrepareUserAsync();

        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();

        var user = await _fixture.PrepareUserAsync();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new InitiateMitIdSignupCommand(user.Id);

        // act
        await mediator.Send(command);
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actual = context.Users.First(x => x.Id == user.Id);

        // assert
        Assert.True(actual.MitIdSignupInitiatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Handle_SignupInitiated_IsAudited()
    {
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();

        var user = await _fixture.PrepareUserAsync();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new InitiateMitIdSignupCommand(user.Id);

        // Act
        await mediator.Send(command);

        // Assert
        var userIdentityAuditLogRepository = scope.ServiceProvider.GetRequiredService<IUserIdentityAuditLogRepository>();
        var actual = await userIdentityAuditLogRepository.GetAsync(new UserId(user.Id));

        Assert.Contains(actual, log => log.Change == UserAuditedChange.UserLoginFederationRequested);
    }
}
