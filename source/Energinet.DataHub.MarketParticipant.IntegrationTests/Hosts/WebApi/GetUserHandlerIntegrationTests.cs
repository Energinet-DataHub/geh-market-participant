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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetUserHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUser_UserFound_ReturnsUser()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var (_, userId, externalUserId) = await _fixture.DatabaseManager.CreateUserAsync();

        var userIdentityMock = new Mock<IUserIdentityRepository>();
        var userIdentity = new UserIdentity(
            new ExternalUserId(externalUserId),
            "expected_name",
            null,
            null,
            DateTimeOffset.UtcNow,
            true);

        userIdentityMock
            .Setup(repository => repository.GetUserIdentitiesAsync(new[] { userIdentity.Id }))
            .ReturnsAsync(new[] { userIdentity });

        scope.Container!.Register(() => userIdentityMock.Object);

        var mediator = scope.GetInstance<IMediator>();
        var command = new GetUserCommand(userId);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        Assert.Equal(userIdentity.Name, actual.Name);
    }

    [Fact]
    public async Task GetUser_UserNotFound_ThrowsNotFoundValidationException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var userIdentityMock = new Mock<IUserIdentityRepository>();
        scope.Container!.Register(() => userIdentityMock.Object);

        var mediator = scope.GetInstance<IMediator>();
        var command = new GetUserCommand(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => mediator.Send(command));
    }

    [Fact]
    public async Task GetUser_ExternalIdentityNotFound_ThrowsNotFoundValidationException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var (_, userId, externalUserId) = await _fixture.DatabaseManager.CreateUserAsync();

        var userIdentityMock = new Mock<IUserIdentityRepository>();
        var userIdentity = new UserIdentity(
            new ExternalUserId(externalUserId),
            "expected_name",
            null,
            null,
            DateTimeOffset.UtcNow,
            true);

        userIdentityMock
            .Setup(repository => repository.GetUserIdentitiesAsync(new[] { userIdentity.Id }))
            .ReturnsAsync(Array.Empty<UserIdentity>());

        scope.Container!.Register(() => userIdentityMock.Object);

        var mediator = scope.GetInstance<IMediator>();
        var command = new GetUserCommand(userId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => mediator.Send(command));
    }
}
