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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.User;
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
public sealed class GetUserOverviewHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserOverviewHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserOverview_GivenActorWithPermissionUsersManage_ReturnsUserOverview()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mock = new Mock<IUserIdentityRepository>();

        mock.Setup(x => x.GetUserIdentitiesAsync(It.IsAny<IEnumerable<ExternalUserId>>()))
            .ReturnsAsync((IEnumerable<ExternalUserId> x) =>
                x.Select(y => new UserIdentity(y, UserStatus.Active, y.ToString(), new EmailAddress("fake@value"), null, DateTimeOffset.UtcNow)));

        scope.Container!.Register(() => mock.Object);

        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture.DatabaseManager.CreateUserAsync();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var filter = new UserOverviewFilterDto(actorId, null, Enumerable.Empty<Guid>(), Array.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == userId));
    }

    [Fact]
    public async Task GetUserOverview_GivenActorWithPermissionUsersManage_ReturnsUserOverviewUsingSearch()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mock = new Mock<IUserIdentityRepository>();
        var (actorId, userId, externalUserId) = await _fixture.DatabaseManager.CreateUserAsync();
        var userIdsToReturn = new List<ExternalUserId>()
        {
           new(externalUserId)
        };
        mock
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(userIdsToReturn.Select(y =>
                new UserIdentity(y, UserStatus.Inactive, y.ToString(), new EmailAddress("fake@value"), null, DateTime.UtcNow)));

        scope.Container!.Register(() => mock.Object);

        var mediator = scope.GetInstance<IMediator>();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var filter = new UserOverviewFilterDto(actorId, "test", Enumerable.Empty<Guid>(), Array.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == userId));
        Assert.Equal(1, actual.TotalUserCount);
    }

    [Fact]
    public async Task GetUserOverview_GivenActiveFilter_ReturnsFilteredUserOverview()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var (actorId, userId, externalUserId) = await _fixture.DatabaseManager.CreateUserAsync();

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), true))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    new ExternalUserId(externalUserId),
                    UserStatus.Active,
                    "fake_value",
                    new EmailAddress("fake@value"),
                    null,
                    DateTime.UtcNow)
            });

        scope.Container!.Register(() => userIdentityRepository.Object);

        var mediator = scope.GetInstance<IMediator>();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var filter = new UserOverviewFilterDto(actorId, "test", Enumerable.Empty<Guid>(), Enumerable.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == userId));
        Assert.Equal(1, actual.TotalUserCount);
    }

    [Fact]
    public async Task GetUserOverview_GivenUserRoleFilter_ReturnsFilteredUserOverview()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var (actorId, userId, externalUserId) = await _fixture.DatabaseManager.CreateUserAsync();

        var userRoleId = await _fixture.DatabaseManager.AddUserPermissionsAsync(actorId, userId, new[]
        {
            Permission.ActorManage
        });

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    new ExternalUserId(externalUserId),
                    UserStatus.Inactive,
                    "fake_value",
                    new EmailAddress("fake@value"),
                    null,
                    DateTime.UtcNow)
            });

        scope.Container!.Register(() => userIdentityRepository.Object);

        var mediator = scope.GetInstance<IMediator>();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var filter = new UserOverviewFilterDto(actorId, "test", Enumerable.Empty<Guid>(), Enumerable.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == userId));
        Assert.Equal(1, actual.TotalUserCount);
    }
}
