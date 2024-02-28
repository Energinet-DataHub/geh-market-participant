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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public class PermissionRelationServiceTest
{
    [Fact]
    public async Task BuildPermissionRelationRecordsAsync()
    {
        // Arrange
        var mockPermissionRepository = new Mock<IPermissionRepository>();
        mockPermissionRepository
            .Setup(e => e.GetAllAsync())
            .ReturnsAsync(BuildTestPermissions);
        var mockUserRoleRepository = new Mock<IUserRoleRepository>();
        mockUserRoleRepository
            .Setup(e => e.GetAllAsync())
            .ReturnsAsync(new List<UserRole>
                {
                    new(
                    "TestRoleBillingAgent",
                    "TestDescription",
                    UserRoleStatus.Active,
                    new List<PermissionId>()
                    {
                        PermissionId.ActorsManage,
                        PermissionId.CalculationsManage,
                    },
                    EicFunction.BillingAgent),
                    new(
                        "TestRoleGridAccessProvider",
                        "TestDescription",
                        UserRoleStatus.Active,
                        new List<PermissionId>()
                        {
                            PermissionId.ActorsManage,
                            PermissionId.CalculationsManage,
                        },
                        EicFunction.GridAccessProvider)
                });

        var permissionRelationService = new PermissionRelationService(mockPermissionRepository.Object, mockUserRoleRepository.Object);

        // Act
        var records = (await permissionRelationService.BuildRelationRecordsAsync()).ToList();

        // Assert
        var testPermissions = BuildTestPermissions().ToList();
        var marketRoles = Enum.GetValues<EicFunction>();
        Assert.Equal(26, records.Count);
        Assert.Equal(2, records.Count(e => e.Permission == PermissionId.ActorsManage.ToString()));
        Assert.Equal(2, records.Count(e => e.Permission == PermissionId.CalculationsManage.ToString()));
        Assert.Equal(testPermissions.Count - 2, records.Count(e => !string.IsNullOrEmpty(e.Permission) && string.IsNullOrEmpty(e.MarketRole) && string.IsNullOrEmpty(e.UserRole)));
        Assert.Equal(marketRoles.Length - 2, records.Count(e => !string.IsNullOrEmpty(e.MarketRole) && string.IsNullOrEmpty(e.Permission) && string.IsNullOrEmpty(e.UserRole)));
    }

    private IEnumerable<Permission> BuildTestPermissions()
    {
        var permissions = Enum.GetValues<PermissionId>();
        foreach (var t in permissions)
        {
            yield return new Permission(
                t,
                t.ToString(),
                $"TestDescription-{t}",
                NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow),
                new List<EicFunction>());
        }
    }
}
