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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

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
    public async Task GetUserAsync_UserDoesntExist_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context);

        // Act
        var user = await userRepository.GetAsync(new ExternalUserId(Guid.Empty));

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserAsync_SimpleUserExist_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context2);

        var email = "fake@mail.com";
        var userExternalId = Guid.NewGuid();
        var userEntity = new UserEntity() { ExternalId = userExternalId, Email = email, RoleAssignments = { } };
        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var user = await userRepository.GetAsync(new ExternalUserId(userExternalId));

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userExternalId, user.ExternalId.Value);
        Assert.NotEqual(Guid.Empty, user.Id.Value);
        Assert.Equal(email, user.Email.Address);
    }

    [Fact]
    public async Task GetUserAsync_UserExist_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserRepository(context2);

        var email = "fake@mail.com";
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
            BusinessRegisterIdentifier = "44444444"
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
            Email = email,
            RoleAssignments = { roleAssignment }
        };
        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        // Act
        var user = await userRepository.GetAsync(new ExternalUserId(userExternalId));

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userExternalId, user.ExternalId.Value);
        Assert.NotEqual(Guid.Empty, user.Id.Value);
        Assert.Equal(email, user.Email.Address);
        Assert.Single(user.RoleAssignments);
        Assert.Equal(userRoleTemplate.Id, user.RoleAssignments.First().TemplateId.Value);
    }
}
