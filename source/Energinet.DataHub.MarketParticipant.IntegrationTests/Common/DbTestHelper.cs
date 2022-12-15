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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

internal static class DbTestHelper
{
    public static async Task<Guid> CreateActorAsync(
        this MarketParticipantDatabaseManager manager,
        EicFunction[] marketRoles)
    {
        await using var context = manager.CreateDbContext();

        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };

        foreach (var eicFunction in marketRoles)
        {
            actorEntity.MarketRoles.Add(new MarketRoleEntity
            {
                Id = Guid.NewGuid(),
                ActorInfoId = actorEntity.Id,
                Function = eicFunction
            });
        }

        var organizationEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Actors = { actorEntity },
            Address = new AddressEntity
            {
                Country = "Denmark"
            },
            Name = string.Empty,
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(organizationEntity);
        await context.SaveChangesAsync();

        return actorEntity.Id;
    }

    public static async Task<(Guid ActorId, Guid UserId, Guid ExternalUserId)> CreateUserAsync(
        this MarketParticipantDatabaseManager manager)
    {
        await using var context = manager.CreateDbContext();

        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };

        var organizationEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Actors = { actorEntity },
            Address = new AddressEntity
            {
                Country = "Denmark"
            },
            Name = string.Empty,
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(organizationEntity);
        await context.SaveChangesAsync();

        var userEntity = new UserEntity
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid(),
            Email = "test@test.test",
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        return (actorEntity.Id, userEntity.Id, userEntity.ExternalId);
    }

    public static async Task<(Guid Actor1Id, Guid Actor2Id, Guid UserId)> CreateUserWithTwoActorsAsync(
        this MarketParticipantDatabaseManager manager)
    {
        await using var context = manager.CreateDbContext();

        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };
        var actor2Entity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active
        };

        var organizationEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Actors = { actorEntity, actor2Entity },
            Address = new AddressEntity
            {
                Country = "Denmark"
            },
            Name = string.Empty,
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(organizationEntity);
        await context.SaveChangesAsync();

        var userEntity = new UserEntity
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid(),
            Email = "test@test.test",
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        return (actorEntity.Id, actor2Entity.Id, userEntity.Id);
    }

    public static Task<UserRoleTemplateId> CreateRoleTemplateAsync(this MarketParticipantDatabaseManager manager)
    {
        return CreateRoleTemplateAsync(manager, new[] { Permission.OrganizationView });
    }

    public static async Task<UserRoleTemplateId> CreateRoleTemplateAsync(
        this MarketParticipantDatabaseManager manager,
        Permission[] permissions)
    {
        await using var context = manager.CreateDbContext();
        var userRoleTemplate = new UserRoleTemplateEntity { Name = "fake_value" };

        foreach (var permission in permissions)
        {
            userRoleTemplate.Permissions.Add(new UserRoleTemplatePermissionEntity
            {
                Permission = permission
            });
        }

        foreach (var eicFunction in new[] { EicFunction.BillingAgent })
        {
            userRoleTemplate.EicFunctions.Add(new UserRoleTemplateEicFunctionEntity
            {
                EicFunction = eicFunction
            });
        }

        context.UserRoleTemplates.Add(userRoleTemplate);
        await context.SaveChangesAsync();
        return new UserRoleTemplateId(userRoleTemplate.Id);
    }

    public static async Task<Guid> AddUserPermissionsAsync(
        this MarketParticipantDatabaseManager manager,
        Guid actorId,
        Guid userId,
        Permission[] permissions)
    {
        await using var context = manager.CreateDbContext();

        var userRoleTemplate = new UserRoleTemplateEntity { Name = "fake_value" };

        foreach (var permission in permissions)
        {
            userRoleTemplate.Permissions.Add(new UserRoleTemplatePermissionEntity
            {
                Permission = permission
            });
        }

        foreach (var eicFunction in new[] { EicFunction.BillingAgent })
        {
            userRoleTemplate.EicFunctions.Add(new UserRoleTemplateEicFunctionEntity
            {
                EicFunction = eicFunction
            });
        }

        context.UserRoleTemplates.Add(userRoleTemplate);
        await context.SaveChangesAsync();

        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorId,
            UserRoleTemplateId = userRoleTemplate.Id
        };

        var userEntity = await context.Users.FindAsync(userId);
        userEntity!.RoleAssignments.Add(roleAssignment);

        context.Users.Update(userEntity);
        await context.SaveChangesAsync();

        return userRoleTemplate.Id;
    }

    public static async Task<(Guid ExternalUserId, Guid ActorId)> CreateUserAsync(
        this MarketParticipantDatabaseManager manager,
        Permission[] permissions)
    {
        await using var context = manager.CreateDbContext();

        var externalUserId = Guid.NewGuid();

        var actorEntity = new ActorEntity
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            ActorNumber = new MockedGln(),
            Status = (int)ActorStatus.Active,
            MarketRoles = { new MarketRoleEntity { Function = EicFunction.ConsentAdministrator } }
        };

        var organizationEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Actors = { actorEntity },
            Address = new AddressEntity
            {
                Country = "Denmark"
            },
            Name = string.Empty,
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(organizationEntity);
        await context.SaveChangesAsync();

        var userRoleTemplate = new UserRoleTemplateEntity();

        foreach (var permission in permissions)
        {
            userRoleTemplate.Permissions.Add(new UserRoleTemplatePermissionEntity
            {
                Permission = permission
            });
        }

        foreach (var eicFunction in new[] { EicFunction.ConsentAdministrator })
        {
            userRoleTemplate.EicFunctions.Add(new UserRoleTemplateEicFunctionEntity
            {
                EicFunction = eicFunction
            });
        }

        context.UserRoleTemplates.Add(userRoleTemplate);
        await context.SaveChangesAsync();

        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleTemplateId = userRoleTemplate.Id
        };

        var userEntity = new UserEntity
        {
            ExternalId = externalUserId,
            Email = "test@test.test",
            RoleAssignments = { roleAssignment }
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        return (externalUserId, actorEntity.Id);
    }
}
