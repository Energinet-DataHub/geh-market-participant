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
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserRoleRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserRoleRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_TemplateDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleTemplateRepository = new UserRoleRepository(context);

        // Act
        var user = await userRoleTemplateRepository.GetAsync(new UserRoleId(Guid.Empty));

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetAsync_HasTemplate_ReturnsTemplate()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRoleTemplateRepository = new UserRoleRepository(context);

        var userRoleTemplateEntity = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent } },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        await context2.UserRoles.AddAsync(userRoleTemplateEntity);
        await context2.SaveChangesAsync();

        // Act
        var userRoleTemplate = await userRoleTemplateRepository.GetAsync(new UserRoleId(userRoleTemplateEntity.Id));

        // Assert
        Assert.NotNull(userRoleTemplate);
        Assert.Equal(userRoleTemplateEntity.Name, userRoleTemplate.Name);
        Assert.Single(userRoleTemplate.AllowedMarkedRoles, EicFunction.Agent);
        Assert.Single(userRoleTemplate.Permissions, Permission.UsersManage);
    }

    [Fact]
    public async Task GetAsync_NoFunctions_ReturnsNothing()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRoleTemplateRepository = new UserRoleRepository(context);

        var userRoleTemplateEntity = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent } },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        await context2.UserRoles.AddAsync(userRoleTemplateEntity);
        await context2.SaveChangesAsync();

        // Act
        var userRoleTemplates = await userRoleTemplateRepository.GetAsync(Array.Empty<EicFunction>());

        // Assert
        Assert.Empty(userRoleTemplates);
    }

    [Fact]
    public async Task GetAsync_TwoFunctions_ReturnsBoth()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRoleTemplateRepository = new UserRoleRepository(context);

        var userRoleTemplateEntity1 = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent } },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        var userRoleTemplateEntity2 = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent } },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        await context2.UserRoles.AddAsync(userRoleTemplateEntity1);
        await context2.UserRoles.AddAsync(userRoleTemplateEntity2);
        await context2.SaveChangesAsync();

        // Act
        var userRoleTemplates = await userRoleTemplateRepository.GetAsync(new[] { EicFunction.Agent });

        // Assert
        Assert.Equal(2, userRoleTemplates.Count());
    }

    [Fact]
    public async Task GetAsync_TwoDifferentFunctions_ReturnsCorrectOne()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRoleTemplateRepository = new UserRoleRepository(context);

        var userRoleTemplateEntity1 = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent } },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        var userRoleTemplateEntity2 = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions = { new UserRoleEicFunctionEntity { EicFunction = EicFunction.BalanceResponsibleParty } },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        await context2.UserRoles.AddAsync(userRoleTemplateEntity1);
        await context2.UserRoles.AddAsync(userRoleTemplateEntity2);
        await context2.SaveChangesAsync();

        // Act
        var userRoleTemplates = await userRoleTemplateRepository.GetAsync(new[] { EicFunction.BalanceResponsibleParty });

        // Assert
        Assert.Equal(userRoleTemplateEntity2.Id, userRoleTemplates.Single().Id.Value);
    }

    [Fact]
    public async Task GetAsync_MultipleFunctions_DoesNotReturnWhenMissingFunction()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRoleTemplateRepository = new UserRoleRepository(context);

        var userRoleTemplateEntity = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "fake_value",
            EicFunctions =
            {
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent },
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.BillingAgent },
            },
            Permissions = { new UserRolePermissionEntity { Permission = Permission.UsersManage } },
        };

        await context2.UserRoles.AddAsync(userRoleTemplateEntity);
        await context2.SaveChangesAsync();

        // Act
        var userRoleTemplates = await userRoleTemplateRepository.GetAsync(new[] { EicFunction.BillingAgent });

        // Assert
        Assert.DoesNotContain(userRoleTemplates, t => t.Id.Value == userRoleTemplateEntity.Id);
    }
}
