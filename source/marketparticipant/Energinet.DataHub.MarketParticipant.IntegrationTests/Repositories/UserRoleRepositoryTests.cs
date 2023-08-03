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
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UserRoleRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserRoleRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_UserRoleDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        // Act
        var user = await userRoleRepository.GetAsync(new UserRoleId(Guid.Empty));

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetAsync_HasUserRole_ReturnsUserRole()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        var userRole = await _fixture.PrepareUserRoleAsync(new[] { PermissionId.UsersManage }, EicFunction.BillingAgent);

        // Act
        var actual = await userRoleRepository.GetAsync(new UserRoleId(userRole.Id));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(userRole.Name, actual.Name);
        Assert.Equal(EicFunction.BillingAgent, actual.EicFunction);
        Assert.Single(actual.Permissions, PermissionId.UsersManage);
    }

    [Fact]
    public async Task GetAsync_NoFunctions_ReturnsNothing()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        await _fixture.PrepareUserRoleAsync(new[] { PermissionId.UsersManage }, EicFunction.BillingAgent);

        // Act
        var actual = await userRoleRepository.GetAsync(Array.Empty<EicFunction>());

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetAsync_TwoFunctions_ReturnsBoth()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        var userRole1 = await _fixture.PrepareUserRoleAsync(new[] { PermissionId.UsersManage }, EicFunction.BillingAgent);
        var userRole2 = await _fixture.PrepareUserRoleAsync(new[] { PermissionId.UsersManage }, EicFunction.BillingAgent);

        // Act
        var actual = await userRoleRepository.GetAsync(new[] { EicFunction.BillingAgent });

        // Assert
        Assert.NotNull(actual.FirstOrDefault(x => x.Id.Value == userRole1.Id));
        Assert.NotNull(actual.FirstOrDefault(x => x.Id.Value == userRole2.Id));
    }

    [Fact]
    public async Task GetAsync_TwoDifferentFunctions_ReturnsCorrectOne()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        await _fixture.PrepareUserRoleAsync(new[] { PermissionId.UsersManage }, EicFunction.BillingAgent);
        var userRole2 = await _fixture.PrepareUserRoleAsync(new[] { PermissionId.UsersManage }, EicFunction.BalanceResponsibleParty);

        // Act
        var actual = await userRoleRepository.GetAsync(new[] { EicFunction.BalanceResponsibleParty });

        // Assert
        Assert.Equal(userRole2.Id, actual.Single().Id.Value);
    }

    [Fact]
    public async Task GetAsync_MultipleFunctions_DoesNotReturnWhenMissingFunction()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.UsersManage },
            EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent);

        // Act
        var actual = await userRoleRepository.GetAsync(new[] { EicFunction.BillingAgent });

        // Assert
        Assert.DoesNotContain(actual, t => t.Id.Value == userRole.Id);
    }

    [Fact]
    public async Task CreateAsync_AllValid_ReturnsUser()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);
        var userRoleRepository2 = new UserRoleRepository(context2);

        var userRole = new UserRole(
            new UserRoleId(Guid.Empty),
            "fake_value",
            "fake_value",
            UserRoleStatus.Active,
            new List<PermissionId>(),
            EicFunction.IndependentAggregator);

        await context2.SaveChangesAsync();

        // Act
        var userRoleId = await userRoleRepository.AddAsync(userRole);

        // Assert
        var actual = await userRoleRepository2.GetAsync(userRoleId);

        Assert.NotNull(actual);
        Assert.NotEqual(Guid.Empty, actual.Id.Value);
        Assert.Equal(userRole.Name, actual.Name);
        Assert.Equal(userRole.Description, actual.Description);
        Assert.Equal(userRole.Status, actual.Status);
        Assert.Equal(userRole.EicFunction, actual.EicFunction);
    }

    [Fact]
    public async Task GetByNameInMarkerRole_NameExistInMarketRole()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        var userRoleNameForUpdate = "Access1";

        var existingUserRole = TestPreparationEntities.ValidUserRole.Patch(e => e.Name = userRoleNameForUpdate);
        var userRole = await _fixture.PrepareUserRoleAsync(existingUserRole);

        // Act
        var actual = await userRoleRepository.GetAsync(new UserRoleId(userRole.Id));

        var useRoleUnderMarketRole = await userRoleRepository
            .GetByNameInMarketRoleAsync(userRoleNameForUpdate, existingUserRole.EicFunctions.First().EicFunction);

        // Assert
        Assert.NotNull(useRoleUnderMarketRole);
        Assert.NotNull(actual);
        Assert.Equal(userRole.Name, actual.Name);
        Assert.Equal(userRole.EicFunctions.First().EicFunction, actual.EicFunction);
    }

    [Fact]
    public async Task GetByNameInMarkerRole_NameDoesNotExistInMarketRole()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleRepository = new UserRoleRepository(context);

        var existingUserRole = TestPreparationEntities.ValidUserRole.Patch(e => e.Name = "Access1");
        existingUserRole.EicFunctions.Clear();
        existingUserRole.EicFunctions.Add(new UserRoleEicFunctionEntity() { EicFunction = EicFunction.EnergySupplier });
        var userRole = await _fixture.PrepareUserRoleAsync(existingUserRole);

        // Act
        var actual = await userRoleRepository.GetAsync(new UserRoleId(userRole.Id));

        var useRoleUnderMarketRole = await userRoleRepository
            .GetByNameInMarketRoleAsync("Access1", EicFunction.BillingAgent);

        // Assert
        Assert.Null(useRoleUnderMarketRole);
        Assert.NotNull(actual);
    }
}
