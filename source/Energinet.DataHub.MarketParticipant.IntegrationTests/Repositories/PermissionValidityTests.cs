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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationPermissionTest")]
[IntegrationTest]
public sealed class PermissionValidityTests
{
    private readonly MarketParticipantPermissionDatabaseFixture _fixture;

    public PermissionValidityTests(MarketParticipantPermissionDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Ensure_All_Permissions_Has_Description_And_EicFunction_Assigned()
    {
        await using var host = await OrganizationIntegrationTestHost.InitializePermissionValidityAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);

        // Act
        var allPermissions = (await permissionRepository.GetAllAsync())
            .Where(x =>
                !x.Description.Contains("fake_test_value", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert
        Assert.Equal(allPermissions.Select(x => x.Permission), Enum.GetValues<Permission>());
#pragma warning disable CA1806
        Assert.All(allPermissions, x => x.EicFunctions.Any());
#pragma warning restore CA1806
    }
}
