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
        Assert.NotNull(actual.FirstOrDefault(x => x.Id.Value == userId));
        Assert.NotNull(actual.FirstOrDefault(x => x.Id.Value == otherUserId));
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
        Assert.NotNull(actual.FirstOrDefault(x => x.Id.Value == userId));
        Assert.Null(actual.FirstOrDefault(x => x.Id.Value == otherUserId));
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
        Assert.Equal(userIds.Select(x => x.UserId).OrderBy(x => x), actual.Select(x => x.Id.Value).OrderBy(x => x));
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
                        new UserIdentity(y, y.ToString(), null, null, DateTime.UtcNow, false))));
        return userIdentityRepository;
    }

    private static async Task<(Guid ActorId, IEnumerable<(Guid UserId, Guid ExternalId)> UserIds)> CreateUsersForSameActorAsync(MarketParticipantDbContext context, int count)
    {
        var (_, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, false);

        var users = new List<(Guid UserId, Guid ExternalId)>();

        for (var i = 0; i < count; ++i)
        {
            var user = await CreateUserAsync(context, actorEntity, userRoleTemplate);
            users.Add((user.Id, user.ExternalId));
        }

        return (actorEntity.Id, users);
    }

    private static async Task<(Guid UserId, Guid ExternalId, Guid ActorId)> CreateUserAndActor(MarketParticipantDbContext context, bool isFas)
    {
        var (_, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas);
        var userEntity = await CreateUserAsync(context, actorEntity, userRoleTemplate);

        return (userEntity.Id, userEntity.ExternalId, actorEntity.Id);
    }

    private static async Task<(OrganizationEntity Organization, ActorEntity Actor, UserRoleEntity Template)> CreateActorAndTemplate(MarketParticipantDbContext context, bool isFas)
    {
        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = "Actor name",
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active,
            IsFas = isFas
        };

        var orgEntity = new OrganizationEntity
        {
            Actors = { actorEntity },
            Address = new AddressEntity
            {
                Country = "DK",
            },
            Name = "Organization name",
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();

        var userRoleTemplate = new UserRoleEntity
        {
            Name = "Template name",
            Permissions = { new UserRolePermissionEntity { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.TransmissionCapacityAllocator } }
        };

        await context.Entry(actorEntity).ReloadAsync();

        return (orgEntity, actorEntity, userRoleTemplate);
    }

    private static async Task<UserEntity> CreateUserAsync(MarketParticipantDbContext context, ActorEntity actorEntity, UserRoleEntity userRole)
    {
        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleId = userRole.Id
        };

        var userEntity = new UserEntity
        {
            ExternalId = Guid.NewGuid(),
            Email = "test@example.com",
            RoleAssignments = { roleAssignment }
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();
        return userEntity;
    }
}
