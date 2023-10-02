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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

internal static class TestUserRolePreparationHelper
{
    public static Task<UserRoleEntity> PrepareUserRoleAsync(
        this MarketParticipantDatabaseFixture fixture)
    {
        return fixture.PrepareUserRoleAsync(TestPreparationEntities.ValidUserRole);
    }

    public static Task<UserRoleEntity> PrepareUserRoleAsync(
        this MarketParticipantDatabaseFixture fixture,
        params PermissionId[] permissions)
    {
        var localUserRole = TestPreparationEntities.ValidUserRole;
        localUserRole.Permissions.Clear();

        foreach (var permission in permissions)
        {
            localUserRole.Permissions.Add(new UserRolePermissionEntity
            {
                Permission = permission,
                ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
            });
        }

        return fixture.PrepareUserRoleAsync(localUserRole);
    }

    public static Task<UserRoleEntity> PrepareUserRoleAsync(
        this MarketParticipantDatabaseFixture fixture,
        PermissionId[] permissions,
        params EicFunction[] functions)
    {
        var localUserRole = TestPreparationEntities.ValidUserRole;
        localUserRole.Permissions.Clear();
        localUserRole.EicFunctions.Clear();

        foreach (var permission in permissions)
        {
            localUserRole.Permissions.Add(new UserRolePermissionEntity
            {
                Permission = permission,
                ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
            });
        }

        foreach (var function in functions)
        {
            localUserRole.EicFunctions.Add(new UserRoleEicFunctionEntity
            {
                EicFunction = function
            });
        }

        return fixture.PrepareUserRoleAsync(localUserRole);
    }

    public static Task<UserRoleEntity> PrepareUserRoleAsync(
        this MarketParticipantDatabaseFixture fixture,
        params EicFunction[] functions)
    {
        var localUserRole = TestPreparationEntities.ValidUserRole;
        localUserRole.EicFunctions.Clear();

        foreach (var function in functions)
        {
            localUserRole.EicFunctions.Add(new UserRoleEicFunctionEntity
            {
                EicFunction = function
            });
        }

        return fixture.PrepareUserRoleAsync(localUserRole);
    }

    public static async Task<UserRoleEntity> PrepareUserRoleAsync(
        this MarketParticipantDatabaseFixture fixture,
        UserRoleEntity userRoleEntity)
    {
        await using var context = fixture.DatabaseManager.CreateDbContext();

        await context.UserRoles.AddAsync(userRoleEntity);
        await context.SaveChangesAsync();

        return userRoleEntity;
    }
}
