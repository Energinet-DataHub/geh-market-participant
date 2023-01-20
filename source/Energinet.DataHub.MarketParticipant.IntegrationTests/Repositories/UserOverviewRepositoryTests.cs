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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using Xunit;
using Xunit.Categories;
using Permission = Energinet.DataHub.Core.App.Common.Security.Permission;
using UserIdentity = Energinet.DataHub.MarketParticipant.Domain.Model.Users.UserIdentity;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
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

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object);

        // Act
        var actual = (await target.GetUsersAsync(1, 1000, null)).ToList();

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

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object);

        // Act
        var actual = (await target.GetUsersAsync(1, 1000, actorId)).ToList();

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

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object);

        // Act
        var userCount = await target.GetTotalUserCountAsync(actorId);
        var actual = new List<UserOverviewItem>();

        for (var i = 0; i < Math.Ceiling(userCount / 7.0); ++i)
        {
            actual.AddRange(await target.GetUsersAsync(i + 1, 7, actorId));
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
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId> { externalId }).Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            actorId,
            null,
            Array.Empty<UserStatus>());

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
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId> { externalId }).Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            null,
            "Axolotl",
            Array.Empty<UserStatus>());

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
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId>()).Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            otherActorId,
            "Axolotl",
            Array.Empty<UserStatus>());

        // Assert
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
    }

    [Fact]
    public async Task SearchUsers_SearchTextMatchesEmail_ReturnsOne()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, _, _) = await CreateUserWithActorName(context, false, "Axolotl");
        var (otherUserId, otherExternalId, _) = await CreateUserWithEmailActorName(context, false, "Bahamut", "alexander@example.com");
        var (otherUser2Id, _, _) = await CreateUserWithEmailActorName(context, false, "Shiva", "shiva@example.com");

        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId> { otherExternalId }).Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            null,
            "Alex",
            Array.Empty<UserStatus>());

        // Assert
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.NotNull(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUser2Id));
    }

    [Fact]
    public async Task SearchUsers_SearchTextMatchesEmailAndActorName_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, externalId, _) = await CreateUserWithActorName(context, false, "Axolotl");
        var (otherUserId, otherExternalId, _) = await CreateUserWithEmailActorName(context, false, "Bahamut", "axol@example.com");
        var (otherUser2Id, _, _) = await CreateUserWithEmailActorName(context, false, "Shiva", "shiva@example.com");

        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId> { externalId, otherExternalId }).Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            null,
            "axol",
            Array.Empty<UserStatus>());

        // Assert
        Assert.NotNull(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.NotNull(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUser2Id));
    }

    [Fact]
    public async Task SearchUsers_SearchTextMatchesEmailAndActorNameBothNotActorId_ReturnsOne()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var (userId, externalId, _) = await CreateUserWithActorName(context, false, "Axolotl");
        var (otherUserId, otherExternalId, otherActorId) = await CreateUserWithEmailActorName(context, false, "Bahamut", "axol@example.com");
        var (otherUser2Id, otherExternal2Id, _) = await CreateUserWithActorName(context, false, "Shiva");

        var target = new UserOverviewRepository(
            context,
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId> { externalId, otherExternal2Id }, new Collection<ExternalUserId> { otherExternalId }).Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            otherActorId,
            "axol",
            Array.Empty<UserStatus>());

        // Assert
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == userId));
        Assert.NotNull(actual.Items.SingleOrDefault(x => x.Id == otherUserId));
        Assert.Null(actual.Items.SingleOrDefault(x => x.Id == otherUser2Id));
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
                new UserIdentity(externalId, UserStatus.Active, "fake_value", new EmailAddress("fake@value"), null, DateTime.UtcNow)
            });

        var target = new UserOverviewRepository(
            context,
            userIdentityRepositoryMock.Object);

        // Act
        var actual = await target.SearchUsersAsync(
            1,
            1000,
            null,
            null,
            new[] { UserStatus.Active });

        // Assert
        Assert.Single(actual.Items, user => user.Id == userId);
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
            CreateUserIdentityRepositoryForSearch(new Collection<ExternalUserId>(), new Collection<ExternalUserId>(userIdList.Select(x => x.ExternalId).ToList())).Object);

        // Act
        var actual = new List<UserOverviewItem>();
        actual.AddRange((await target.SearchUsersAsync(1, 8, actorId, "Name", Array.Empty<UserStatus>())).Items);
        actual.AddRange((await target.SearchUsersAsync(2, 8, actorId, "Name", Array.Empty<UserStatus>())).Items);
        actual.AddRange((await target.SearchUsersAsync(3, 8, actorId, "Name", Array.Empty<UserStatus>())).Items);

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

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object);

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

        var target = new UserOverviewRepository(context, CreateUserIdentityRepository().Object);

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
                    x.Select(y =>
                        new UserIdentity(y, UserStatus.Inactive, y.ToString(), new EmailAddress("fake@value"), null, DateTime.UtcNow))));
        return userIdentityRepository;
    }

    private static Mock<IUserIdentityRepository> CreateUserIdentityRepositoryForSearch(IReadOnlyCollection<ExternalUserId> userIdsToReturnFromSearch, IReadOnlyCollection<ExternalUserId> userIdsToReturnFromGet)
    {
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.SearchUserIdentitiesAsync(It.IsAny<string>(), null))
            .ReturnsAsync(userIdsToReturnFromSearch.Select(y =>
                new UserIdentity(y, UserStatus.Inactive, y.ToString(), new EmailAddress("fake@value"), null, DateTime.UtcNow)));

        userIdentityRepository
            .Setup(x => x.GetUserIdentitiesAsync(It.IsAny<IEnumerable<ExternalUserId>>()))
            .Returns<IEnumerable<ExternalUserId>>((_) =>
                Task.FromResult(
                    userIdsToReturnFromGet.Select(y =>
                        new UserIdentity(y, UserStatus.Inactive, y.ToString(), new EmailAddress("fake@value"), null, DateTime.UtcNow))));
        return userIdentityRepository;
    }

    private static async Task<(Guid ActorId, IEnumerable<(UserId UserId, ExternalUserId ExternalId)> UserIds)> CreateUsersForSameActorAsync(MarketParticipantDbContext context, int count)
    {
        var (_, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, false);

        var users = new List<(UserId UserId, ExternalUserId ExternalId)>();

        for (var i = 0; i < count; ++i)
        {
            var user = await CreateUserAsync(context, actorEntity, userRoleTemplate);
            users.Add((new UserId(user.Id), new ExternalUserId(user.ExternalId)));
        }

        return (actorEntity.Id, users);
    }

    private static async Task<(UserId UserId, ExternalUserId ExternalId, Guid ActorId)> CreateUserAndActor(
        MarketParticipantDbContext context, bool isFas)
    {
        var (_, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas);
        var userEntity = await CreateUserAsync(context, actorEntity, userRoleTemplate);
        return (new UserId(userEntity.Id), new ExternalUserId(userEntity.ExternalId), actorEntity.Id);
    }

    private static async Task<(UserId UserId, ExternalUserId ExternalId, Guid ActorId)> CreateUserWithEicFunction(
        MarketParticipantDbContext context, bool isFas, EicFunction eicFunction)
    {
        var (_, actorEntity, roles) = await CreateActorAndTwoTemplates(context, isFas, eicFunction: eicFunction);
        var userEntity = await CreateUserWithMultipleRolesAsync(context, actorEntity, roles);
        return (new UserId(userEntity.Id), new ExternalUserId(userEntity.ExternalId), actorEntity.Id);
    }

    private static async Task<(UserId UserId, ExternalUserId ExternalId, Guid ActorId)> CreateUserWithActorName(
        MarketParticipantDbContext context, bool isFas, string actorName)
    {
        var (_, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas, actorName);
        var userEntity = await CreateUserAsync(context, actorEntity, userRoleTemplate);
        return (new UserId(userEntity.Id), new ExternalUserId(userEntity.ExternalId), actorEntity.Id);
    }

    private static async Task<(UserId UserId, ExternalUserId ExternalId, Guid ActorId)> CreateUserWithEmailActorName(
        MarketParticipantDbContext context, bool isFas, string actorName, string email)
    {
        var (_, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas, actorName);
        var userEntity = await CreateUserAsync(context, actorEntity, userRoleTemplate, email);
        return (new UserId(userEntity.Id), new ExternalUserId(userEntity.ExternalId), actorEntity.Id);
    }

    private static async Task<(OrganizationEntity Organization, ActorEntity Actor, UserRoleEntity Template)> CreateActorAndTemplate(
            MarketParticipantDbContext context,
            bool isFas,
            string actorName = "Actor name",
            EicFunction eicFunction = EicFunction.TransmissionCapacityAllocator)
    {
        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = actorName,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active,
            IsFas = isFas
        };

        var orgEntity = new OrganizationEntity
        {
            Actors = { actorEntity },
            Address = new AddressEntity { Country = "DK" },
            Name = "Organization name",
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();

        var userRoleTemplate = new UserRoleEntity
        {
            Name = "Template name",
            Permissions = { new UserRolePermissionEntity { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = eicFunction } }
        };
        await context.UserRoles.AddAsync(userRoleTemplate);
        await context.SaveChangesAsync();
        await context.Entry(actorEntity).ReloadAsync();

        return (orgEntity, actorEntity, userRoleTemplate);
    }

    private static async Task<(OrganizationEntity Organization, ActorEntity Actor, List<UserRoleEntity> UserRoles)> CreateActorAndTwoTemplates(
        MarketParticipantDbContext context,
        bool isFas,
        string actorName = "Actor name",
        EicFunction eicFunction = EicFunction.TransmissionCapacityAllocator)
    {
        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = actorName,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active,
            IsFas = isFas
        };

        var orgEntity = new OrganizationEntity
        {
            Actors = { actorEntity },
            Address = new AddressEntity { Country = "DK" },
            Name = "Organization name",
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();

        var userRoleTemplate = new UserRoleEntity
        {
            Name = "Template name",
            Permissions = { new UserRolePermissionEntity { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = eicFunction } }
        };
        var userRoleTemplate2 = new UserRoleEntity
        {
            Name = "Template name 2",
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = eicFunction } }
        };
        await context.UserRoles.AddAsync(userRoleTemplate);
        await context.UserRoles.AddAsync(userRoleTemplate2);

        await context.SaveChangesAsync();
        await context.Entry(actorEntity).ReloadAsync();

        var roles = new List<UserRoleEntity>()
        {
            userRoleTemplate, userRoleTemplate2
        };
        return (orgEntity, actorEntity, roles);
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
            ExternalId = Guid.NewGuid(),
            Email = email,
            RoleAssignments = { roleAssignment }
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();
        return userEntity;
    }

    private static async Task<UserEntity> CreateUserWithMultipleRolesAsync(MarketParticipantDbContext context, ActorEntity actorEntity, List<UserRoleEntity> userRoles)
    {
        var assignments = userRoles.Select(userRole => new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleId = userRole.Id
        })
            .ToList();

        var userEntity = new UserEntity
        {
            ExternalId = Guid.NewGuid(),
            Email = "test@example.com",
            RoleAssignments = new Collection<UserRoleAssignmentEntity>(assignments)
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();
        return userEntity;
    }
}
