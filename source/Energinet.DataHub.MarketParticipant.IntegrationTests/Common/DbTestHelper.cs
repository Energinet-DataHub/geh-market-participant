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
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

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
            Domain = new MockedDomain(),
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
            Domain = new MockedDomain(),
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
            Domain = new MockedDomain(),
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

    public static Task<UserRoleId> CreateRoleTemplateAsync(this MarketParticipantDatabaseManager manager)
    {
        return CreateRoleTemplateAsync(manager, new[] { Permission.OrganizationView });
    }

    public static async Task<UserRoleId> CreateRoleTemplateAsync(
        this MarketParticipantDatabaseManager manager,
        Permission[] permissions)
    {
        await using var context = manager.CreateDbContext();
        var userRoleTemplate = new UserRoleEntity { Name = "fake_value" };

        foreach (var permission in permissions)
        {
            userRoleTemplate.Permissions.Add(new UserRolePermissionEntity
            {
                Permission = permission
            });
        }

        foreach (var eicFunction in new[] { EicFunction.BillingAgent })
        {
            userRoleTemplate.EicFunctions.Add(new UserRoleEicFunctionEntity
            {
                EicFunction = eicFunction
            });
        }

        context.UserRoles.Add(userRoleTemplate);
        await context.SaveChangesAsync();
        return new UserRoleId(userRoleTemplate.Id);
    }

    public static async Task<UserRoleId> CreateUserRoleAsync(
        this MarketParticipantDatabaseManager manager,
        string name,
        string description,
        UserRoleStatus status,
        EicFunction eicFunction,
        Permission[] permissions)
    {
        await using var context = manager.CreateDbContext();
        var userRoleEntity = new UserRoleEntity { Name = name, Description = description, Status = status };

        foreach (var permission in permissions)
        {
            userRoleEntity.Permissions.Add(new UserRolePermissionEntity
            {
                Permission = permission
            });
        }

        userRoleEntity.EicFunctions.Add(new UserRoleEicFunctionEntity
        {
            EicFunction = eicFunction
        });

        context.UserRoles.Add(userRoleEntity);
        await context.SaveChangesAsync();
        return new UserRoleId(userRoleEntity.Id);
    }

    public static async Task<Guid> AddUserPermissionsAsync(
        this MarketParticipantDatabaseManager manager,
        Guid actorId,
        Guid userId,
        Permission[] permissions)
    {
        await using var context = manager.CreateDbContext();

        var userRoleTemplate = new UserRoleEntity { Name = "fake_value" };

        foreach (var permission in permissions)
        {
            userRoleTemplate.Permissions.Add(new UserRolePermissionEntity
            {
                Permission = permission
            });
        }

        foreach (var eicFunction in new[] { EicFunction.BillingAgent })
        {
            userRoleTemplate.EicFunctions.Add(new UserRoleEicFunctionEntity
            {
                EicFunction = eicFunction
            });
        }

        context.UserRoles.Add(userRoleTemplate);
        await context.SaveChangesAsync();

        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorId,
            UserRoleId = userRoleTemplate.Id
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
            MarketRoles = { new MarketRoleEntity { Function = EicFunction.IndependentAggregator } }
        };

        var organizationEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Actors = { actorEntity },
            Address = new AddressEntity
            {
                Country = "Denmark"
            },
            Domain = new MockedDomain(),
            Name = string.Empty,
            BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier
        };

        await context.Organizations.AddAsync(organizationEntity);
        await context.SaveChangesAsync();

        var userRoleTemplate = new UserRoleEntity();

        foreach (var permission in permissions)
        {
            userRoleTemplate.Permissions.Add(new UserRolePermissionEntity
            {
                Permission = permission
            });
        }

        foreach (var eicFunction in new[] { EicFunction.IndependentAggregator })
        {
            userRoleTemplate.EicFunctions.Add(new UserRoleEicFunctionEntity
            {
                EicFunction = eicFunction
            });
        }

        context.UserRoles.Add(userRoleTemplate);
        await context.SaveChangesAsync();

        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleId = userRoleTemplate.Id
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

    public static async Task<int> CreateEmailEventAsync(
        this MarketParticipantDatabaseManager manager,
        EmailAddress emailAddress,
        EmailEventType emailEventType)
    {
        await using var context = manager.CreateDbContext();

        var emailEventEntity = new EmailEventEntity()
        {
            Created = DateTimeOffset.UtcNow,
            Email = emailAddress.Address,
            EmailEventType = (int)emailEventType
        };

        await context.EmailEventEntries.AddAsync(emailEventEntity).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return emailEventEntity.Id;
    }

    public static async Task EmailEventsClearNotSentAsync(this MarketParticipantDatabaseManager manager)
    {
        await using var context = manager.CreateDbContext();

        await context.EmailEventEntries
            .Where(e => e.Sent == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Sent, x => DateTimeOffset.UtcNow))
            .ConfigureAwait(false);

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
