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

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class AllowedPermissionsForUserRoleRuleServiceIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public AllowedPermissionsForUserRoleRuleServiceIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task ValidateUserRolePermissions_PermissionsAllowed_DoesNotThrow()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var target = new AllowedPermissionsForUserRoleRuleService(new PermissionRepository(context));
        var userRole = new UserRole(
            "User Role Name",
            "User Role Description",
            UserRoleStatus.Active,
            [PermissionId.UsersView],
            EicFunction.GridAccessProvider);

        // Act + Assert
        await target.ValidateUserRolePermissionsAsync(userRole);
    }

    [Fact]
    public async Task ValidateUserRolePermissions_PermissionsDisallowed_ThrowsException()
    {
        // Arrange
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var target = new AllowedPermissionsForUserRoleRuleService(new PermissionRepository(context));
        var userRole = new UserRole(
            "User Role Name",
            "User Role Description",
            UserRoleStatus.Active,
            [PermissionId.ActorsManage],
            EicFunction.GridAccessProvider);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.ValidateUserRolePermissionsAsync(userRole));
    }
}
