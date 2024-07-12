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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UniqueUserRoleNameRuleServiceIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public UniqueUserRoleNameRuleServiceIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task ValidateUserRoleName_NameAvailable_DoesNotThrow()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userRoleEntity = TestPreparationEntities.ValidUserRole.Patch(ur => ur.Name = Guid.NewGuid().ToString());
        await _databaseFixture.PrepareUserRoleAsync(userRoleEntity);

        var target = new UniqueUserRoleNameRuleService(new UserRoleRepository(context));

        // Act + Assert
        await target.ValidateUserRoleNameAsync(new UserRole(
            Guid.NewGuid().ToString(),
            "desc",
            UserRoleStatus.Active,
            Array.Empty<PermissionId>(),
            userRoleEntity.EicFunctions.Single().EicFunction));
    }

    [Fact]
    public async Task ValidateUserRoleName_NameInUse_ThrowsException()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userRoleEntity = TestPreparationEntities.ValidUserRole.Patch(ur => ur.Name = Guid.NewGuid().ToString());
        await _databaseFixture.PrepareUserRoleAsync(userRoleEntity);

        var target = new UniqueUserRoleNameRuleService(new UserRoleRepository(context));

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.ValidateUserRoleNameAsync(new UserRole(
            userRoleEntity.Name,
            "desc",
            UserRoleStatus.Active,
            Array.Empty<PermissionId>(),
            userRoleEntity.EicFunctions.Single().EicFunction)));
    }

    [Fact]
    public async Task ValidateUserRoleName_NameInUseInAnotherMarketRole_DoesNotThrow()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userRoleEntity = TestPreparationEntities.ValidUserRole.Patch(ur => ur.Name = Guid.NewGuid().ToString());
        await _databaseFixture.PrepareUserRoleAsync(userRoleEntity);

        var target = new UniqueUserRoleNameRuleService(new UserRoleRepository(context));

        // Act + Assert
        await target.ValidateUserRoleNameAsync(new UserRole(
            userRoleEntity.Name,
            "desc",
            UserRoleStatus.Active,
            Array.Empty<PermissionId>(),
            EicFunction.DataHubAdministrator));
    }

    [Fact]
    public async Task ValidateUserRoleName_NameInUseInInactiveRole_DoesNotThrow()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userRoleEntity = TestPreparationEntities.ValidUserRole
            .Patch(ur => ur.Name = Guid.NewGuid().ToString())
            .Patch(ur => ur.Status = UserRoleStatus.Inactive);

        await _databaseFixture.PrepareUserRoleAsync(userRoleEntity);

        var target = new UniqueUserRoleNameRuleService(new UserRoleRepository(context));

        // Act + Assert
        await target.ValidateUserRoleNameAsync(new UserRole(
            userRoleEntity.Name,
            "desc",
            UserRoleStatus.Active,
            Array.Empty<PermissionId>(),
            userRoleEntity.EicFunctions.Single().EicFunction));
    }

    [Fact]
    public async Task ValidateUserRoleName_RenamingExistingRole_DoesNotThrow()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userRoleEntity = TestPreparationEntities.ValidUserRole.Patch(ur => ur.Name = Guid.NewGuid().ToString());
        await _databaseFixture.PrepareUserRoleAsync(userRoleEntity);

        var repository = new UserRoleRepository(context);
        var target = new UniqueUserRoleNameRuleService(repository);

        var existingUserRole = await repository.GetAsync(new UserRoleId(userRoleEntity.Id));
        existingUserRole!.Description = "new_desc";

        // Act + Assert
        await target.ValidateUserRoleNameAsync(existingUserRole);
    }
}
