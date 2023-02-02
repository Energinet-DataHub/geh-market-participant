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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
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
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
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
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Agent, PermissionId = (int)Permission.UsersManage
        };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = new Collection<PermissionEicFunctionEntity>() { permissionEicFunction },
            Description = "Test description"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository.GetAllAsync()).ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Single(permissions);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "Test description", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => p.EicFunctions.Count() == 1);
        Assert.Equal(EicFunction.Agent, permissions.First().EicFunctions.First());
    }

    [Fact]
    public async Task GetAllAsync_OnePermissionsWithMultipleEicFunctionsExist_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Agent
        };
        var permissionEicFunction2 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Scheduling
        };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = new Collection<PermissionEicFunctionEntity>() { permissionEicFunction, permissionEicFunction2 },
            Description = "Test description"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository.GetAllAsync()).ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Single(permissions);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "Test description", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => p.EicFunctions.Count() == 2);
        Assert.Equal(EicFunction.Agent, permissions.First().EicFunctions.First());
        Assert.Equal(EicFunction.Scheduling, permissions.First().EicFunctions.Skip(1).First());
    }

    [Fact]
    public async Task GetAllAsync_MultiplePermissionsWithMultipleEicFunctionsExist_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Agent
        };
        var permissionEicFunction2 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Scheduling
        };

        var permissionEicFunction3 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Agent
        };
        var permissionEicFunction4 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Scheduling
        };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = new Collection<PermissionEicFunctionEntity>() { permissionEicFunction, permissionEicFunction2 },
            Description = "Test description"
        };

        var permission2 = new PermissionEntity()
        {
            Id = (int)Permission.ActorManage,
            EicFunctions = new Collection<PermissionEicFunctionEntity>() { permissionEicFunction3, permissionEicFunction4 },
            Description = "Test description 2"
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
        Assert.Contains(permissions, p => string.Equals(p.Description, "Test description", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => string.Equals(p.Description, "Test description 2", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, permissions.First().EicFunctions.Count());
        Assert.Equal(2, permissions.Skip(1).First().EicFunctions.Count());
        Assert.Equal(EicFunction.Agent, permissions.First().EicFunctions.First());
        Assert.Equal(EicFunction.Scheduling, permissions.First().EicFunctions.Skip(1).First());
        Assert.Equal(EicFunction.Agent, permissions.Skip(1).First().EicFunctions.First());
        Assert.Equal(EicFunction.Scheduling, permissions.Skip(1).First().EicFunctions.Skip(1).First());
    }

    [Fact]
    public async Task GetToMarketRoleAsync_MultiplePermissionsWithMultipleEicFunctionsExist_ReturnsPermissionToCorrecEicFunctionWithCorrectDetails()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var permissionEicFunction = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Agent
        };
        var permissionEicFunction2 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Scheduling
        };

        var permissionEicFunction3 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.GridAccessProvider
        };
        var permissionEicFunction4 = new PermissionEicFunctionEntity()
        {
            EicFunction = EicFunction.Scheduling
        };
        var permission = new PermissionEntity()
        {
            Id = (int)Permission.UsersManage,
            EicFunctions = new Collection<PermissionEicFunctionEntity>() { permissionEicFunction, permissionEicFunction2 },
            Description = "Test description"
        };

        var permission2 = new PermissionEntity()
        {
            Id = (int)Permission.ActorManage,
            EicFunctions = new Collection<PermissionEicFunctionEntity>() { permissionEicFunction3, permissionEicFunction4 },
            Description = "Test description 2"
        };

        await context2.Permissions.AddAsync(permission);
        await context2.Permissions.AddAsync(permission2);
        await context2.SaveChangesAsync();

        // Act
        var permissions = (await permissionRepository.GetToMarketRoleAsync(EicFunction.Agent)).ToList();
        var permissions2 = (await permissionRepository.GetToMarketRoleAsync(EicFunction.GridAccessProvider)).ToList();

        // Assert
        Assert.NotEmpty(permissions);
        Assert.Single(permissions);
        Assert.Contains(permissions, p => p.Permission == Permission.UsersManage);
        Assert.Contains(permissions, p => string.Equals(p.Description, "Test description", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, permissions.First().EicFunctions.Count());
        Assert.Equal(EicFunction.Agent, permissions.First().EicFunctions.First());
        Assert.Equal(EicFunction.Scheduling, permissions.First().EicFunctions.Skip(1).First());

        Assert.NotEmpty(permissions2);
        Assert.Single(permissions2);
        Assert.Contains(permissions2, p => p.Permission == Permission.ActorManage);
        Assert.Contains(permissions2, p => string.Equals(p.Description, "Test description 2", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, permissions2.First().EicFunctions.Count());
        Assert.Equal(EicFunction.GridAccessProvider, permissions2.First().EicFunctions.First());
        Assert.Equal(EicFunction.Scheduling, permissions2.First().EicFunctions.Skip(1).First());
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var allPermissions = await context.Permissions.ToListAsync();
        context.Permissions.RemoveRange(allPermissions);
        await context.SaveChangesAsync();
    }
}
