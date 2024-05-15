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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class PermissionRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public PermissionRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_AllPermissions_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        // Act
        var actual = (await permissionRepository
            .GetAllAsync())
            .ToList();

        // Assert
        foreach (var knownPermission in KnownPermissions.All)
        {
            var returnedPermission = actual.Single(p => p.Id == knownPermission.Id);

            Assert.Equal(knownPermission.Id, returnedPermission.Id);
            Assert.Equal(knownPermission.Claim, returnedPermission.Claim);
            Assert.Equal(knownPermission.Created, returnedPermission.Created);
            Assert.Equal(knownPermission.AssignableTo, returnedPermission.AssignableTo);
        }
    }

    [Fact]
    public async Task GetAsync_SinglePermission_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        // Act
        var actual = await permissionRepository.GetAsync(PermissionId.ActorsManage);

        // Assert
        var organizationViewPermission = KnownPermissions.All.Single(kp => kp.Id == PermissionId.ActorsManage);
        Assert.NotNull(actual);
        Assert.Equal(organizationViewPermission.Id, actual.Id);
        Assert.Equal(organizationViewPermission.Claim, actual.Claim);
        Assert.Equal(organizationViewPermission.Created, actual.Created);
        Assert.Equal(organizationViewPermission.AssignableTo, actual.AssignableTo);
    }

    [Fact]
    public async Task GetAsync_MultiplePermissions_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        // Act
        var actual = (await permissionRepository
            .GetAsync(KnownPermissions.All.Select(kp => kp.Id)))
            .ToList();

        // Assert
        foreach (var knownPermission in KnownPermissions.All)
        {
            var returnedPermission = actual.Single(p => p.Id == knownPermission.Id);

            Assert.Equal(knownPermission.Id, returnedPermission.Id);
            Assert.Equal(knownPermission.Claim, returnedPermission.Claim);
            Assert.Equal(knownPermission.Created, returnedPermission.Created);
            Assert.Equal(knownPermission.AssignableTo, returnedPermission.AssignableTo);
        }
    }

    [Fact]
    public async Task GetForMarketRoleAsync_MultiplePermissions_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        // Act
        var actual = (await permissionRepository
            .GetForMarketRoleAsync(EicFunction.DataHubAdministrator))
            .ToList();

        // Assert
        foreach (var knownPermission in KnownPermissions.All.Where(kp => kp.AssignableTo.Contains(EicFunction.DataHubAdministrator)))
        {
            var returnedPermission = actual.Single(p => p.Id == knownPermission.Id);

            Assert.Equal(knownPermission.Id, returnedPermission.Id);
            Assert.Equal(knownPermission.Claim, returnedPermission.Claim);
            Assert.Equal(knownPermission.Created, returnedPermission.Created);
            Assert.Equal(knownPermission.AssignableTo, returnedPermission.AssignableTo);
        }
    }

    [Fact]
    public async Task UpdatePermissionAsync_NewDescription_ReturnsPermissionWithCorrectDetails()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        var initialPermission = await permissionRepository.GetAsync(PermissionId.ActorsManage);
        Assert.NotNull(initialPermission);

        // Act
        initialPermission.Description = $"{Guid.NewGuid()}";
        await permissionRepository.UpdatePermissionAsync(initialPermission);

        // Assert
        var actual = await permissionRepository.GetAsync(PermissionId.ActorsManage);
        Assert.NotNull(actual);
        Assert.Equal(initialPermission.Description, actual.Description);
    }
}
