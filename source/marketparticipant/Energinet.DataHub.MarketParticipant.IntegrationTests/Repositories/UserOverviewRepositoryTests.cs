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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using Xunit;
using Xunit.Categories;
using UserIdentity = Energinet.DataHub.MarketParticipant.Domain.Model.Users.UserIdentity;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UserOverviewRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    public UserOverviewRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsers_NoActorId_ReturnsAllUsers()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, _, _) = await CreateUserAndActor(context, false);
        var (otherUserId, _, _) = await CreateUserAndActor(context, false);

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object, new UserStatusCalculator());

        // Act
        var actual = (await target.GetUsersAsync(1, 1000, UserOverviewSortProperty.Email, SortDirection.Asc, null)).ToList();

        // Assert
        Assert.NotNull(actual.FirstOrDefault(x => x.Id == userId));
        Assert.NotNull(actual.FirstOrDefault(x => x.Id == otherUserId));
    }

    [Fact]
    public async Task GetUsers_ActorIdProvided_ReturnsOnlyUsersUnderActor()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, _, actorId) = await CreateUserAndActor(context, false);
        var (otherUserId, _, _) = await CreateUserAndActor(context, false);

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object, new UserStatusCalculator());

        // Act
        var actual = (await target.GetUsersAsync(1, 1000, UserOverviewSortProperty.Email, SortDirection.Asc, actorId)).ToList();

        // Assert
        Assert.NotNull(actual.FirstOrDefault(x => x.Id == userId));
        Assert.Null(actual.FirstOrDefault(x => x.Id == otherUserId));
    }

    [Fact]
    public async Task GetUsers_ActorIdProvided_PagesResults()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (actorId, userIds) = await CreateUsersForSameActorAsync(context, 100);

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object, new UserStatusCalculator());

        // Act
        var userCount = await target.GetTotalUserCountAsync(actorId);
        var actual = new List<UserOverviewItem>();

        for (var i = 0; i < Math.Ceiling(userCount / 7.0); ++i)
        {
            actual.AddRange(await target.GetUsersAsync(i + 1, 7, UserOverviewSortProperty.Email, SortDirection.Asc, actorId));
        }

        // Assert
        Assert.Equal(userIds.Select(x => x.UserId).OrderBy(x => x.Value), actual.Select(x => x.Id).OrderBy(x => x.Value));
    }

    [Fact]
    public async Task SearchUsers_ActorIdProvidedAndNoOtherSearchParameters_ReturnsOnlyUsersUnderActor()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, externalId, actorId) = await CreateUserAndActor(context, false);
        var (otherUserId, _, _) = await CreateUserAndActor(context, false);

        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId> { externalId }).Object,
            new UserStatusCalculator());

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            UserOverviewSortProperty.Email,
            SortDirection.Asc,
            actorId,
            null,
            Array.Empty<UserStatus>(),
            Array.Empty<UserRoleId>());

        // Assert
        Assert.NotNull(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
    }

    [Fact]
    public async Task SearchUsers_ActorNameParam_ReturnsOnlyUsersWithName()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, externalId, _) = await CreateUserWithActorName(context, false, "Axolotl");
        var (otherUserId, _, _) = await CreateUserWithActorName(context, false, "Bahamut");

        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId> { externalId }).Object,
            new UserStatusCalculator());

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            UserOverviewSortProperty.Email,
            SortDirection.Asc,
            null,
            "Axolotl",
            Array.Empty<UserStatus>(),
            Array.Empty<UserRoleId>());

        // Assert
        Assert.NotNull(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
    }

    [Fact]
    public async Task SearchUsers_ActorNameParamWithWrongActor_ReturnsNone()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, _, _) = await CreateUserWithActorName(context, false, "Axolotl");
        var (otherUserId, _, otherActorId) = await CreateUserWithActorName(context, false, "Bahamut");

        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId>()).Object,
            new UserStatusCalculator());

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            UserOverviewSortProperty.Email,
            SortDirection.Asc,
            otherActorId,
            "Axolotl",
            Array.Empty<UserStatus>(),
            Array.Empty<UserRoleId>());

        // Assert
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
    }

    [Fact]
    public async Task SearchUsers_OnlyActiveUsers_ReturnsExpectedUser()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, externalId, _) = await CreateUserWithActorName(context, false, "Axolotl");

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        userIdentityRepositoryMock
            .Setup(x => x.SearchUserIdentitiesAsync(null, true))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    externalId,
                    new MockedEmailAddress(),
                    UserIdentityStatus.Active,
                    "fake_value",
                    "fake_value",
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())
            });

        var target = new UserOverviewRepository(
            context,
            userIdentityRepositoryMock.Object,
            new UserStatusCalculator());

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            UserOverviewSortProperty.Email,
            SortDirection.Asc,
            null,
            null,
            new[] { UserStatus.Active },
            Array.Empty<UserRoleId>());

        // Assert
        Assert.Single(actual.Items, user => user.Id == userId);
    }

    [Fact]
    public async Task SearchUsers_OnlyUsersForGivenRole_ReturnsExpectedUser()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, externalId, actorId) = await CreateUserWithActorName(context, false, "Axolotl");

        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);
        await _fixture.AssignUserRoleAsync(userId.Value, actorId.Value, userRole.Id);

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        userIdentityRepositoryMock
            .Setup(x => x.SearchUserIdentitiesAsync(null, null))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    externalId,
                    new MockedEmailAddress(),
                    UserIdentityStatus.Active,
                    "fake_value",
                    "fake_value",
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())
            });

        var target = new UserOverviewRepository(
            context,
            userIdentityRepositoryMock.Object,
            new UserStatusCalculator());

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            UserOverviewSortProperty.Email,
            SortDirection.Asc,
            null,
            null,
            Enumerable.Empty<UserStatus>(),
            new[] { new UserRoleId(userRole.Id) });

        // Assert
        Assert.Single(actual.Items, user => user.Id == userId);
    }

    [Fact]
    public async Task SearchUsers_NoUserRolesButAdministratedBy_ReturnsUser()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorEntity = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t =>
            {
                t.Name = "fake_value";
            }));

        var userEntity = await _fixture.PrepareUserAsync(
            TestPreparationEntities.UnconnectedUser.Patch(t => t.AdministratedByActorId = actorEntity.Id));

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        userIdentityRepositoryMock
            .Setup(x => x.SearchUserIdentitiesAsync(null, null))
            .ReturnsAsync(new[]
            {
                new UserIdentity(
                    new ExternalUserId(userEntity.ExternalId),
                    new MockedEmailAddress(),
                    UserIdentityStatus.Active,
                    "fake_value",
                    "fake_value",
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())
            });

        var target = new UserOverviewRepository(
            context,
            userIdentityRepositoryMock.Object,
            new UserStatusCalculator());

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            UserOverviewSortProperty.Email,
            SortDirection.Asc,
            null,
            null,
            Enumerable.Empty<UserStatus>(),
            Enumerable.Empty<UserRoleId>());

        // Assert
        Assert.Single(actual.Items, user => user.Id.Value == userEntity.Id);
    }

    [Fact]
    public async Task SearchUsers_PagesResults()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var (actorId, userIds) = await CreateUsersForSameActorAsync(context, 20);
        var userIdList = userIds.ToList();
        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId>(userIdList.Select(x => x.ExternalId).ToList())).Object,
            new UserStatusCalculator());

        // Act
        var actual = new List<UserOverviewItem>();
        actual.AddRange((await target.SearchUsersAsync(1, 8, UserOverviewSortProperty.Email, SortDirection.Asc, actorId, "Name", Array.Empty<UserStatus>(), Enumerable.Empty<UserRoleId>())).Items);
        actual.AddRange((await target.SearchUsersAsync(2, 8, UserOverviewSortProperty.Email, SortDirection.Asc, actorId, "Name", Array.Empty<UserStatus>(), Enumerable.Empty<UserRoleId>())).Items);
        actual.AddRange((await target.SearchUsersAsync(3, 8, UserOverviewSortProperty.Email, SortDirection.Asc, actorId, "Name", Array.Empty<UserStatus>(), Enumerable.Empty<UserRoleId>())).Items);

        // Assert
        Assert.Equal(userIdList.Select(x => x.UserId).OrderBy(x => x.Value), actual.Select(x => x.Id).OrderBy(x => x.Value));
    }

    [Fact]
    public async Task GetTotalUserCount_GivenActor_ReturnsCount()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        const int userCount = 7;

        var (actorId, _) = await CreateUsersForSameActorAsync(context, userCount);

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object, new UserStatusCalculator());

        // Act
        var actual = await target.GetTotalUserCountAsync(actorId);

        // Assert
        Assert.Equal(userCount, actual);
    }

    [Fact]
    public async Task GetTotalUserCount_WithoutActor_ReturnsCount()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        await CreateUsersForSameActorAsync(context, 1);

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object, new UserStatusCalculator());

        // Act
        var actual = await target.GetTotalUserCountAsync(null);

        // Assert
        Assert.True(actual >= 1);
    }

    private static Mock<IUserIdentityRepository> CreateUserIdentityRepository()
    {
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.GetUserIdentitiesAsync(It.IsAny<IEnumerable<ExternalUserId>>()))
            .Returns<IEnumerable<ExternalUserId>>(x =>
                Task.FromResult(
                    x.Select(y => new UserIdentity(
                        y,
                        new EmailAddress($"{y}@test.datahub.dk"),
                        UserIdentityStatus.Inactive,
                        y.ToString(),
                        y.ToString(),
                        null,
                        DateTime.UtcNow,
                        AuthenticationMethod.Undetermined,
                        new List<LoginIdentity>()))));

        return userIdentityRepository;
    }

    private static Mock<IUserIdentityRepository> CreateUserIdentityRepositoryForSearch(IReadOnlyCollection<ExternalUserId> userIdsToReturnFromSearch, IReadOnlyCollection<ExternalUserId> userIdsToReturnFromGet)
    {
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(userIdsToReturnFromSearch.Select(y =>
                new UserIdentity(
                    y,
                    new EmailAddress($"{y}@test.datahub.dk"),
                    UserIdentityStatus.Inactive,
                    y.ToString(),
                    y.ToString(),
                    null,
                    DateTime.UtcNow,
                    AuthenticationMethod.Undetermined,
                    new List<LoginIdentity>())));

        userIdentityRepository
            .Setup(x => x.GetUserIdentitiesAsync(It.IsAny<IEnumerable<ExternalUserId>>()))
            .Returns<IEnumerable<ExternalUserId>>((_) =>
                Task.FromResult(
                    userIdsToReturnFromGet.Select(y =>
                        new UserIdentity(
                            y,
                            new EmailAddress($"{y}@test.datahub.dk"),
                            UserIdentityStatus.Inactive,
                            y.ToString(),
                            y.ToString(),
                            null,
                            DateTime.UtcNow,
                            AuthenticationMethod.Undetermined,
                            new List<LoginIdentity>()))));

        return userIdentityRepository;
    }

    private static async Task<UserEntity> CreateUserAsync(MarketParticipantDbContext context, ActorEntity actorEntity, UserRoleEntity userRole, string email = "test@example.com")
    {
        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleId = userRole.Id
        };

        var userEntity = new UserEntity
        {
            AdministratedByActorId = actorEntity.Id,
            ExternalId = Guid.NewGuid(),
            Email = email,
            RoleAssignments = { roleAssignment }
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();
        return userEntity;
    }

    private async Task<(ActorId ActorId, IEnumerable<(UserId UserId, ExternalUserId ExternalId)> UserIds)> CreateUsersForSameActorAsync(MarketParticipantDbContext context, int count)
    {
        var (actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, false);

        var users = new List<(UserId UserId, ExternalUserId ExternalId)>();

        for (var i = 0; i < count; ++i)
        {
            var user = await CreateUserAsync(context, actorEntity, userRoleTemplate);
            users.Add((new UserId(user.Id), new ExternalUserId(user.ExternalId)));
        }

        return (new ActorId(actorEntity.Id), users);
    }

    private async Task<(UserId UserId, ExternalUserId ExternalId, ActorId ActorId)> CreateUserAndActor(
        MarketParticipantDbContext context, bool isFas)
    {
        var (actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas);
        var userEntity = await CreateUserAsync(context, actorEntity, userRoleTemplate);
        return (new UserId(userEntity.Id), new ExternalUserId(userEntity.ExternalId), new ActorId(actorEntity.Id));
    }

    private async Task<(UserId UserId, ExternalUserId ExternalId, ActorId ActorId)> CreateUserWithActorName(
        MarketParticipantDbContext context, bool isFas, string actorName)
    {
        var (actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas, actorName);
        var userEntity = await CreateUserAsync(context, actorEntity, userRoleTemplate);
        return (new UserId(userEntity.Id), new ExternalUserId(userEntity.ExternalId), new ActorId(actorEntity.Id));
    }

    private async Task<(ActorEntity Actor, UserRoleEntity Template)> CreateActorAndTemplate(
            MarketParticipantDbContext context,
            bool isFas,
            string actorName = "Actor name",
            EicFunction eicFunction = EicFunction.BillingAgent)
    {
        var actorEntity = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t =>
            {
                t.IsFas = isFas;
                t.Name = actorName;
            }));

        var userRoleTemplate = new UserRoleEntity
        {
            Name = "Template name",
            Permissions = { new UserRolePermissionEntity { Permission = PermissionId.OrganizationsManage, ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value } },
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = eicFunction } },
            ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value
        };

        await context.UserRoles.AddAsync(userRoleTemplate);
        await context.SaveChangesAsync();
        await context.Entry(actorEntity).ReloadAsync();

        return (actorEntity, userRoleTemplate);
    }
}
