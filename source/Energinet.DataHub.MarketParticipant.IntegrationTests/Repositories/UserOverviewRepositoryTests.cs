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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Slim;

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

        var (userId, _) = await CreateUserAndActor(context, false);
        var (otherUserId, _) = await CreateUserAndActor(context, false);
        var target = new UserOverviewRepository(context);

        // Act
        var actual = await target.GetUsersAsync(1, 1000, null);

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

        var (userId, actorId) = await CreateUserAndActor(context, false);
        var (otherUserId, otherActorId) = await CreateUserAndActor(context, false);

        var target = new UserOverviewRepository(context);

        // Act
        var actual = await target.GetUsersAsync(1, 1000, actorId);

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

        var target = new UserOverviewRepository(context);
        var users = new List<UserOverviewItem>();

        // Act
        var pageCount = await target.GetUsersPageCountAsync(7, actorId);
        var actual = new List<UserOverviewItem>();
        for (var i = 0; i < pageCount; ++i)
        {
            actual.AddRange(await target.GetUsersAsync(i + 1, 7, actorId));
        }

        // Assert
        Assert.Equal(userIds.OrderBy(x => x), actual.Select(x => x.Id.Value).OrderBy(x => x));
    }

    private static async Task<(Guid ActorId, IEnumerable<Guid> UserIds)> CreateUsersForSameActorAsync(MarketParticipantDbContext context, int count)
    {
        var (userExternalId, orgEntity, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, false);

        var users = new List<Guid>();

        for (var i = 0; i < count; ++i)
        {
            users.Add((await CreateUserAsync(context, userExternalId, actorEntity, userRoleTemplate)).Id);
        }

        return (actorEntity.Id, users);
    }

    private static async Task<(Guid UserId, Guid ActorId)> CreateUserAndActor(MarketParticipantDbContext context, bool isFas)
    {
        var (userExternalId, _, actorEntity, userRoleTemplate) = await CreateActorAndTemplate(context, isFas);
        var userEntity = await CreateUserAsync(context, userExternalId, actorEntity, userRoleTemplate);

        return (userEntity.Id, actorEntity.Id);
    }

    private static async Task<(Guid UserId, OrganizationEntity Organization, ActorEntity Actor, UserRoleTemplateEntity Template)> CreateActorAndTemplate(MarketParticipantDbContext context, bool isFas)
    {
        var userExternalId = Guid.NewGuid();

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

        var userRoleTemplate = new UserRoleTemplateEntity
        {
            Name = "Template name",
            Permissions = { new UserRoleTemplatePermissionEntity { Permission = Permission.OrganizationManage } },
            EicFunctions = { new UserRoleTemplateEicFunctionEntity { EicFunction = EicFunction.TransmissionCapacityAllocator } }
        };

        await context.Entry(actorEntity).ReloadAsync();

        return (userExternalId, orgEntity, actorEntity, userRoleTemplate);
    }

    private static async Task<UserEntity> CreateUserAsync(MarketParticipantDbContext context, Guid userExternalId, ActorEntity actorEntity, UserRoleTemplateEntity userRoleTemplate)
    {
        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorEntity.Id,
            UserRoleTemplate = userRoleTemplate
        };

        var userEntity = new UserEntity
        {
            ExternalId = userExternalId,
            Email = "test@example.com",
            RoleAssignments = { roleAssignment }
        };

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();
        return userEntity;
    }
}
