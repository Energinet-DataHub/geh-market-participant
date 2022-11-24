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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Slim;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Slim;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetActorsAsync_NoUser_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();

        // Act
        var actorIds = (await userRepository
            .GetActorsAsync(new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.Empty(actorIds);
    }

    [Fact]
    public async Task GetActorsAsync_NoActor_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();
        var userEntity = new UserEntity
        {
            ExternalId = userExternalId,
            Email = "fake@mail.com"
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var actorIds = (await userRepository
            .GetActorsAsync(new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.Empty(actorIds);
    }

    [Fact]
    public async Task GetActorsAsync_WithActor_ReturnsCorrectId()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();
        var externalActorId = Guid.NewGuid();
        var actorEntity = new ActorEntity()
        {
            Id = Guid.NewGuid(),
            ActorId = externalActorId,
            Name = "Test Actor",
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };
        var orgEntity = new OrganizationEntity()
        {
            Actors = { actorEntity },
            Address = new AddressEntity
            {
                City = "test city",
                Country = "Denmark",
                Number = "1",
                StreetName = "Teststreet",
                ZipCode = "1234"
            },
            Name = "Test Org",
            BusinessRegisterIdentifier = "22222222"
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();

        var userRoleTemplate = new UserRoleTemplateEntity
        {
            Name = "Test Template",
            Permissions = { new UserRoleTemplatePermissionEntity { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity { EicFunction = EicFunction.BillingAgent } }
        };

        await context.Entry(actorEntity).ReloadAsync();

        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleTemplate = userRoleTemplate
        };

        var userEntity = new UserEntity
        {
            ExternalId = userExternalId,
            Email = "fake@mail.com",
            RoleAssignments = { roleAssignment }
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var actorIds = (await userRepository
            .GetActorsAsync(new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(actorIds);
        Assert.Single(actorIds);
        Assert.Equal(externalActorId, actorIds.First().Value);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserDoesNotExist_ReturnsEmptyPermissions()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        // Act
        var perms = await userRepository
            .GetPermissionsAsync(new ExternalActorId(Guid.NewGuid()), new ExternalUserId(Guid.NewGuid()));

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithNoPermissions_ReturnsZeroPermissions()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();
        var userEntity = new UserEntity() { ExternalId = userExternalId, Email = "fake@mail.com", RoleAssignments = { } };
        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var perms = await userRepository
            .GetPermissionsAsync(new ExternalActorId(Guid.NewGuid()), new ExternalUserId(userExternalId));

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithPermissions_ReturnsPermissions()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();
        var externalActorId = Guid.NewGuid();
        var actorEntity = new ActorEntity()
        {
            Id = Guid.NewGuid(),
            ActorId = externalActorId,
            Name = "Test Actor",
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };
        var orgEntity = new OrganizationEntity()
        {
            Actors = { actorEntity },
            Address = new AddressEntity()
            {
                City = "test city",
                Country = "Denmark",
                Number = "1",
                StreetName = "Teststreet",
                ZipCode = "1234"
            },
            Name = "Test Org",
            BusinessRegisterIdentifier = "11111111"
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();
        var userRoleTemplate = new UserRoleTemplateEntity()
        {
            Name = "Test Template",
            Permissions = { new UserRoleTemplatePermissionEntity() { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity() { EicFunction = EicFunction.BillingAgent } }
        };
        await context.Entry(actorEntity).ReloadAsync();
        var roleAssignment = new UserRoleAssignmentEntity()
        {
            ActorId = actorEntity.Id,
            UserRoleTemplate = userRoleTemplate
        };
        var userEntity = new UserEntity()
        {
            ExternalId = userExternalId,
            Email = "fake@mail.com",
            RoleAssignments = { roleAssignment }
        };
        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(new ExternalActorId(externalActorId), new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(perms);
        Assert.Equal(Permission.OrganizationManage, perms.First());
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithPermissionsForMultipleActors_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();
        var externalActorId = Guid.NewGuid();
        var externalActor2Id = Guid.NewGuid();
        var actorEntity = new ActorEntity()
        {
            Id = Guid.NewGuid(),
            ActorId = externalActorId,
            Name = "Test Actor",
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };
        var actor2Entity = new ActorEntity()
        {
            Id = Guid.NewGuid(),
            ActorId = externalActor2Id,
            Name = "Test Actor 2",
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };
        var orgEntity = new OrganizationEntity()
        {
            Actors = { actorEntity, actor2Entity },
            Address = new AddressEntity()
            {
                City = "test city",
                Country = "Denmark",
                Number = "1",
                StreetName = "Teststreet",
                ZipCode = "1234"
            },
            Name = "Test Org",
            BusinessRegisterIdentifier = "22222222"
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();
        var userRoleTemplate = new UserRoleTemplateEntity()
        {
            Name = "Test Template",
            Permissions = { new UserRoleTemplatePermissionEntity() { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity() { EicFunction = EicFunction.BillingAgent } }
        };
        var userRoleTemplate2 = new UserRoleTemplateEntity()
        {
            Name = "Test Template 2",
            Permissions = { new UserRoleTemplatePermissionEntity() { Permission = Permission.GridAreasManage } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity() { EicFunction = EicFunction.EnergySupplier } }
        };
        await context.Entry(actorEntity).ReloadAsync();
        await context.Entry(actor2Entity).ReloadAsync();
        var roleAssignment = new UserRoleAssignmentEntity()
        {
            ActorId = actorEntity.Id,
            UserRoleTemplate = userRoleTemplate
        };
        var roleAssignment2 = new UserRoleAssignmentEntity()
        {
            ActorId = actor2Entity.Id,
            UserRoleTemplate = userRoleTemplate2
        };
        var userEntity = new UserEntity()
        {
            ExternalId = userExternalId,
            Email = "fake@mail.com",
            RoleAssignments = { roleAssignment, roleAssignment2 }
        };
        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var permsActor = (await userRepository
            .GetPermissionsAsync(new ExternalActorId(externalActorId), new ExternalUserId(userExternalId)))
            .ToList();

        var permsActor2 = (await userRepository
            .GetPermissionsAsync(new ExternalActorId(externalActor2Id), new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(permsActor);
        Assert.NotEmpty(permsActor2);
        Assert.Single(permsActor);
        Assert.Single(permsActor2);
        Assert.Equal(Permission.OrganizationManage, permsActor.First());
        Assert.Equal(Permission.GridAreasManage, permsActor2.First());
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithMultiplePermissionsForActor_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        var userExternalId = Guid.NewGuid();
        var externalActorId = Guid.NewGuid();
        var actorEntity = new ActorEntity()
        {
            Id = Guid.NewGuid(),
            ActorId = externalActorId,
            Name = "Test Actor",
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };
        var orgEntity = new OrganizationEntity()
        {
            Actors = { actorEntity },
            Address = new AddressEntity()
            {
                City = "test city",
                Country = "Denmark",
                Number = "1",
                StreetName = "Teststreet",
                ZipCode = "1234"
            },
            Name = "Test Org",
            BusinessRegisterIdentifier = "33333333"
        };

        await context.Organizations.AddAsync(orgEntity);
        await context.SaveChangesAsync();
        var userRoleTemplate = new UserRoleTemplateEntity()
        {
            Name = "Test Template",
            Permissions = { new UserRoleTemplatePermissionEntity() { Permission = Permission.OrganizationManage }, new UserRoleTemplatePermissionEntity() { Permission = Permission.OrganizationView } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity() { EicFunction = EicFunction.BillingAgent } }
        };
        var userRoleTemplate2 = new UserRoleTemplateEntity()
        {
            Name = "Test Template 2",
            Permissions = { new UserRoleTemplatePermissionEntity() { Permission = Permission.GridAreasManage } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity() { EicFunction = EicFunction.EnergySupplier } }
        };
        await context.Entry(actorEntity).ReloadAsync();
        var roleAssignment = new UserRoleAssignmentEntity()
        {
            ActorId = actorEntity.Id,
            UserRoleTemplate = userRoleTemplate
        };
        var roleAssignment2 = new UserRoleAssignmentEntity()
        {
            ActorId = actorEntity.Id,
            UserRoleTemplate = userRoleTemplate2
        };
        var userEntity = new UserEntity()
        {
            ExternalId = userExternalId,
            Email = "fake@mail.com",
            RoleAssignments = { roleAssignment, roleAssignment2 }
        };
        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var permsActor = (await userRepository
            .GetPermissionsAsync(new ExternalActorId(externalActorId), new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(permsActor);
        Assert.Equal(3, permsActor.Count);
        Assert.Contains(Permission.OrganizationManage, permsActor);
        Assert.Contains(Permission.OrganizationView, permsActor);
        Assert.Contains(Permission.GridAreasManage, permsActor);
    }
}
