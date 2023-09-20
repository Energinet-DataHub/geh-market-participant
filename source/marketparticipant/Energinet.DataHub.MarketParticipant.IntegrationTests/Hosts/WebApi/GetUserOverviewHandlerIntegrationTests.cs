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
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.User;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
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

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository.Setup(x => x.GetUserIdentitiesAsync(It.IsAny<IEnumerable<ExternalUserId>>()))
            .ReturnsAsync((IEnumerable<ExternalUserId> x) =>
                x.Select(y => new UserIdentity(
                    y,
                    new MockedEmailAddress(),
                    UserIdentityStatus.Active,
                    y.ToString(),
                    y.ToString(),
                    null,
                    DateTimeOffset.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())));

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var filter = new UserOverviewFilterDto(actor.Id, null, Enumerable.Empty<Guid>(), Array.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == user.Id));
    }

    [Fact]
    public async Task GetUserOverview_GivenActorWithPermissionUsersManage_ReturnsUserOverviewUsingSearch()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var actor = await _fixture.PrepareActorAsync();

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var userIdsToReturn = new List<ExternalUserId>
        {
           new(user.ExternalId)
        };

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(userIdsToReturn.Select(y =>
                new UserIdentity(
                    y,
                    new MockedEmailAddress(),
                    UserIdentityStatus.Inactive,
                    y.ToString(),
                    y.ToString(),
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())));

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var filter = new UserOverviewFilterDto(actor.Id, "test", Enumerable.Empty<Guid>(), Array.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == user.Id));
        Assert.Equal(1, actual.TotalUserCount);
    }

    [Fact]
    public async Task GetUserOverview_GivenActiveFilter_ReturnsFilteredUserOverview()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        
        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), true))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    new ExternalUserId(user.ExternalId),
                    new MockedEmailAddress(),
                    UserIdentityStatus.Active,
                    "fake_value",
                    "fake_value",
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())
            });

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var filter = new UserOverviewFilterDto(actor.Id, "test", Enumerable.Empty<Guid>(), new[] { UserStatus.Active });
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == user.Id));
        Assert.Equal(1, actual.TotalUserCount);
    }

    [Fact]
    public async Task GetUserOverview_GivenUserRoleFilter_ReturnsFilteredUserOverview()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRoleA = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);
        var userRoleB = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleA.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleB.Id);

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    new ExternalUserId(user.ExternalId),
                    new MockedEmailAddress(),
                    UserIdentityStatus.Inactive,
                    "fake_value",
                    "fake_value",
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())
            });

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var filter = new UserOverviewFilterDto(actor.Id, "test", new[] { userRoleA.Id }, Enumerable.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.NotNull(actual.Users.First(x => x.Id == user.Id));
        Assert.Equal(1, actual.TotalUserCount);
    }

    [Fact]
    public async Task GetUserOverview_CalculatedUserStatus_ReturnsUserOverviewWithExpectedUserStatus()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var userInvitedSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1));
        var userInvited = await _fixture.PrepareUserAsync(userInvitedSetup);

        var userInvitedButExpiredInActiveSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1));
        var userInvitedButExpiredInActive = await _fixture.PrepareUserAsync(userInvitedButExpiredInActiveSetup);

        var userInvitedButExpiredActiveSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1));
        var userInvitedButExpiredActive = await _fixture.PrepareUserAsync(userInvitedButExpiredActiveSetup);

        var userActiveSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = null);
        var userActive = await _fixture.PrepareUserAsync(userActiveSetup);

        var userInActiveSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = null);
        var userInActive = await _fixture.PrepareUserAsync(userInActiveSetup);

        var actor = await _fixture.PrepareActorAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(userInvited.Id, actor.Id, userRole.Id);
        await _fixture.AssignUserRoleAsync(userInvitedButExpiredActive.Id, actor.Id, userRole.Id);
        await _fixture.AssignUserRoleAsync(userInvitedButExpiredInActive.Id, actor.Id, userRole.Id);
        await _fixture.AssignUserRoleAsync(userActive.Id, actor.Id, userRole.Id);
        await _fixture.AssignUserRoleAsync(userInActive.Id, actor.Id, userRole.Id);

        UserIdentity UserIdentity(UserEntity userEntity, UserIdentityStatus userStatus)
        {
            return new UserIdentity(
                new ExternalUserId(userEntity.ExternalId),
                new MockedEmailAddress(),
                userStatus,
                "fake_value",
                "fake_value",
                null,
                DateTime.UtcNow,
                AuthenticationMethod.Undetermined,
                new List<LoginIdentity>());
        }

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(new[]
            {
                UserIdentity(userInvited, UserIdentityStatus.Active),
                UserIdentity(userInvitedButExpiredActive, UserIdentityStatus.Active),
                UserIdentity(userInvitedButExpiredInActive, UserIdentityStatus.Inactive),
                UserIdentity(userActive, UserIdentityStatus.Active),
                UserIdentity(userInActive, UserIdentityStatus.Inactive)
            });

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var filter = new UserOverviewFilterDto(actor.Id, "test", Enumerable.Empty<Guid>(), Enumerable.Empty<UserStatus>());
        var command = new GetUserOverviewCommand(filter, 1, 100, Application.Commands.Query.User.UserOverviewSortProperty.Email, Application.Commands.SortDirection.Asc);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotEmpty(actual.Users);
        Assert.Equal(5, actual.TotalUserCount);
        Assert.NotNull(actual.Users.Single(x => x.Id == userInvited.Id));
        Assert.True(actual.Users.Single(x => x.Id == userInvited.Id).Status == UserStatus.Invited);
        Assert.NotNull(actual.Users.Single(x => x.Id == userInvitedButExpiredActive.Id));
        Assert.True(actual.Users.Single(x => x.Id == userInvitedButExpiredActive.Id).Status == UserStatus.InviteExpired);
        Assert.NotNull(actual.Users.Single(x => x.Id == userInvitedButExpiredInActive.Id));
        Assert.True(actual.Users.Single(x => x.Id == userInvitedButExpiredInActive.Id).Status == UserStatus.InviteExpired);
        Assert.NotNull(actual.Users.Single(x => x.Id == userActive.Id));
        Assert.True(actual.Users.Single(x => x.Id == userActive.Id).Status == UserStatus.Active);
        Assert.NotNull(actual.Users.Single(x => x.Id == userInActive.Id));
        Assert.True(actual.Users.Single(x => x.Id == userInActive.Id).Status == UserStatus.Inactive);
    }
}
