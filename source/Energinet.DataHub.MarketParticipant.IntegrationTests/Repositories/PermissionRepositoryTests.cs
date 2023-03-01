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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("PermissionIntegrationTest")]
[IntegrationTest]
public sealed class PermissionRepositoryTests : IAsyncLifetime
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public PermissionRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_NoPermissionsExist_ReturnsEmpty()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        // Act
        var permissions = await permissionRepository.GetAllAsync();

        // Assert
        Assert.Empty(permissions);
    }

    [Fact]
    public async Task GetAllAsync_OnePermissionsWithOneEicFunctionExist_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.BillingAgent,
            PermissionId = (int)Permission.UsersManage
        };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = { permissionEicFunction },
            Description = "fake_test_value"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository.GetAllAsync()).ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Single(permissions);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "fake_test_value", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => p.EicFunctions.Count() == 1);
        Assert.Equal(EicFunction.BillingAgent, permissions.First().EicFunctions.First());
    }

    [Fact]
    public async Task GetAllAsync_OnePermissionsWithMultipleEicFunctionsExist_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity() { EicFunction = EicFunction.BillingAgent };
        var permissionEicFunction2 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.ElOverblik };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = { permissionEicFunction, permissionEicFunction2 },
            Description = "fake_test_value"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository
                .GetAllAsync())
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Description) &&
                x.Description.Equals("fake_test_value", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Single(permissions);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "fake_test_value", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => p.EicFunctions.Count() == 2);
        Assert.Equal(EicFunction.BillingAgent, permissions.First().EicFunctions.First());
        Assert.Equal(EicFunction.ElOverblik, permissions.First().EicFunctions.Skip(1).First());
    }

    [Fact]
    public async Task GetAllAsync_MultiplePermissionsWithMultipleEicFunctionsExist_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity() { EicFunction = EicFunction.BillingAgent };
        var permissionEicFunction2 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.ElOverblik };

        var permissionEicFunction3 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.BillingAgent };
        var permissionEicFunction4 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.ElOverblik };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = { permissionEicFunction, permissionEicFunction2 },
            Description = "fake_test_value"
        };

        var permission2 = new PermissionEntity()
        {
            Id = (int)Permission.ActorManage,
            EicFunctions = { permissionEicFunction3, permissionEicFunction4 },
            Description = "fake_test_value2"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.Permissions.AddAsync(permission2);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository.GetAllAsync()).ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Equal(2, permissions.Count);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => p.Permission == Permission.ActorManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "fake_test_value", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => string.Equals(p.Description, "fake_test_value2", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, permissions.First().EicFunctions.Count());
        Assert.Equal(2, permissions.Skip(1).First().EicFunctions.Count());
        Assert.Equal(EicFunction.BillingAgent, permissions.First().EicFunctions.First());
        Assert.Equal(EicFunction.ElOverblik, permissions.First().EicFunctions.Skip(1).First());
        Assert.Equal(EicFunction.BillingAgent, permissions.Skip(1).First().EicFunctions.First());
        Assert.Equal(EicFunction.ElOverblik, permissions.Skip(1).First().EicFunctions.Skip(1).First());
    }

    [Fact]
    public async Task GetToMarketRoleAsync_MultiplePermissionsWithMultipleEicFunctionsExist_ReturnsPermissionToCorrectEicFunctionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity() { EicFunction = EicFunction.BillingAgent };
        var permissionEicFunction2 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.ElOverblik };

        var permissionEicFunction3 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.GridAccessProvider };
        var permissionEicFunction4 = new PermissionEicFunctionEntity() { EicFunction = EicFunction.ElOverblik };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = { permissionEicFunction, permissionEicFunction2 },
            Description = "fake_test_value"
        };

        var permission2 = new PermissionEntity()
        {
            Id = (int)Permission.ActorManage,
            EicFunctions = { permissionEicFunction3, permissionEicFunction4 },
            Description = "fake_test_value2"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.Permissions.AddAsync(permission2);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository.GetToMarketRoleAsync(EicFunction.BillingAgent))
            .ToList();

        var permissions2 = (await permissionRepository.GetToMarketRoleAsync(EicFunction.GridAccessProvider))
            .ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Single(permissions);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "fake_test_value", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, permissions.First().EicFunctions.Count());
        Assert.Equal(EicFunction.BillingAgent, permissions.First().EicFunctions.First());
        Assert.Equal(EicFunction.ElOverblik, permissions.First().EicFunctions.Skip(1).First());

        Assert.NotEmpty(permissions2);
        Assert.Single(permissions2);
        Assert.Contains(permissions2, p => p.Permission == Permission.ActorManage);
        Assert.Contains(permissions2, p => string.Equals(p.Description, "fake_test_value2", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, permissions2.First().EicFunctions.Count());
        Assert.Equal(EicFunction.GridAccessProvider, permissions2.First().EicFunctions.First());
        Assert.Equal(EicFunction.ElOverblik, permissions2.First().EicFunctions.Skip(1).First());
    }

    [Fact]
    public async Task UpdatePermission_Success()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);
        var permissionRepositoryAssert = new PermissionRepository(context2);

        var permission = new PermissionEntity
        {
            Id = (int)Permission.UserRoleManage,
            EicFunctions =
            {
                new PermissionEicFunctionEntity()
                {
                    EicFunction = EicFunction.SystemOperator, PermissionId = (int)Permission.UserRoleManage
                }
            },
            Description = "DescriptionInit"
        };

        await context.Permissions.AddAsync(permission).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        const string newPermissionDescription = "newPermissionDescription";

        // Act
        var permissions = await permissionRepository
            .GetToMarketRoleAsync(EicFunction.SystemOperator)
            .ConfigureAwait(false);
        var permissionDetails = permissions.ToList();
        var permissionToUpdate = permissionDetails.First(p =>
            p.Permission == Permission.UserRoleManage && p.Description == "DescriptionInit");

        permissionToUpdate.Description = newPermissionDescription;

        await permissionRepository
            .UpdatePermissionAsync(permissionToUpdate)
            .ConfigureAwait(false);

        // Assert
        var permissionUpdated = await permissionRepositoryAssert
            .GetToMarketRoleAsync(EicFunction.SystemOperator)
            .ConfigureAwait(false);
        var permissionUpdatedList = permissionUpdated.ToList();
        Assert.Single(permissionUpdatedList);
        Assert.Equal(newPermissionDescription, permissionUpdatedList[0].Description);
    }

    public async Task InitializeAsync()
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var allPermissions = await context.Permissions.ToListAsync();
        context.Permissions.RemoveRange(allPermissions);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var allPermissions =
            await context.Permissions.Where(x => x.Description.Contains("fake_test_value")).ToListAsync();
        context.Permissions.RemoveRange(allPermissions);
        await context.SaveChangesAsync();
    }
}
