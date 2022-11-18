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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserRepositoryTests : IAsyncLifetime
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly Address _validAddress = new(
        "test Street",
        "1",
        "1111",
        "Test City",
        "Test Country");

    public UserRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddOrUpdateAsync_SimpleUserAdded_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);
        var userRepository2 = new UserRepository(context2);
        var testUser = new User("Test User", Guid.NewGuid(), new List<UserActor>());

        // Act
        var userId = await userRepository.AddOrUpdateAsync(testUser);
        var newUser = await userRepository2.GetAsync(userId);

        // Assert
        Assert.NotNull(newUser);
        Assert.NotEqual(Guid.Empty, newUser?.Id);
        Assert.Equal(testUser.Name, newUser?.Name);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UserWithActorAdded_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);
        var userRepository2 = new UserRepository(context2);
        var orgRepository = new OrganizationRepository(context);
        var testActor = new Actor(new MockedGln());
        var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress);
        organization.Actors.Add(testActor);
        var orgId = orgRepository.AddOrUpdateAsync(organization);

        var userActor = new UserActor() { ActorId = testActor.Id };
        var testUser = new User("Test User", Guid.NewGuid(), new List<UserActor>() { userActor });

        // Act
        var userId = await userRepository.AddOrUpdateAsync(testUser);
        var newUser = await userRepository2.GetAsync(userId);

        // Assert
        Assert.NotNull(newUser);
        Assert.NotEqual(Guid.Empty, newUser!.Id);
        Assert.Equal(testUser.Name, newUser!.Name);
        Assert.Single(newUser!.Actors);
        Assert.Equal(testActor.Id,  newUser.Actors.First().ActorId);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UserWithTwoActorsAdded_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);
        var userRepository2 = new UserRepository(context2);
        var orgRepository = new OrganizationRepository(context);

        var testActor = new Actor(new MockedGln());
        var testActor2 = new Actor(new MockedGln());

        var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress);
        organization.Actors.Add(testActor);
        organization.Actors.Add(testActor2);
        await orgRepository.AddOrUpdateAsync(organization);

        var userActor = new UserActor() { ActorId = testActor.Id };
        var userActor2 = new UserActor() { ActorId = testActor2.Id };
        var testUser = new User("Test User", Guid.NewGuid(), new List<UserActor>() { userActor, userActor2 });

        // Act
        var userId = await userRepository.AddOrUpdateAsync(testUser);
        var newUser = await userRepository2.GetAsync(userId);

        // Assert
        Assert.NotNull(newUser);
        Assert.NotEqual(Guid.Empty, newUser!.Id);
        Assert.Equal(testUser.Name, newUser!.Name);
        Assert.Equal(2, newUser!.Actors.Count());
        Assert.Equal(testActor.Id,  newUser.Actors.First().ActorId);
        Assert.Equal(testActor2.Id,  newUser.Actors.Skip(1).First().ActorId);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UserWithActorAndMarketRoleAdded_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);
        var userRoleTemplateRepository = new UserRoleTemplateRepository(context);
        var userRepository2 = new UserRepository(context2);
        var orgRepository = new OrganizationRepository(context);

        var testActor = new Actor(new MockedGln());
        var testMarketRole = new ActorMarketRole(EicFunction.Consumer, new List<ActorGridArea>());
        testActor.MarketRoles.Add(testMarketRole);
        var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress);
        organization.Actors.Add(testActor);
        var orgId = orgRepository.AddOrUpdateAsync(organization);

        var testRole = new UserRoleTemplate("ATest", new List<UserRolePermission>() { new() { PermissionId = "perm1" }, new() { PermissionId = "perm2" } });
        var testRoleId = await userRoleTemplateRepository.AddOrUpdateAsync(testRole);
        var testUserActorRole = new UserActorUserRole() { UserRoleTemplateId = testRoleId };
        var testUserActor = new UserActor() { ActorId = testActor.Id, UserRoles = new List<UserActorUserRole>() { testUserActorRole } };
        var testUser = new User("Test User", Guid.NewGuid(), new List<UserActor>() { testUserActor });

        // Act
        var userId = await userRepository.AddOrUpdateAsync(testUser);
        var newUser = await userRepository2.GetAsync(userId);

        // Assert
        Assert.NotNull(newUser);
        Assert.NotEqual(Guid.Empty, newUser!.Id);
        Assert.Equal(testUser.Name, newUser!.Name);
        Assert.Single(newUser!.Actors);
        Assert.Single(newUser!.Actors.First().UserRoles);
        Assert.Equal(testActor.Id,  newUser.Actors.First().ActorId);
        Assert.Equal(testRoleId, newUser!.Actors.First().UserRoles.First().UserRoleTemplateId);
    }

    public async Task InitializeAsync()
    {
        // Permissions are needed for all user/userRole tests, and they are the same, so are initialized here
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        context.Permissions.Add(new PermissionEntity("perm1", "Test 1"));
        context.Permissions.Add(new PermissionEntity("perm2", "Test 2"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // Permissions are needed for all user/userRole tests, and they are the same, so are initialized here
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        context.Permissions.Remove(new PermissionEntity("perm1", "Test 1"));
        context.Permissions.Remove(new PermissionEntity("perm2", "Test 2"));
        await context.SaveChangesAsync();
    }
}
