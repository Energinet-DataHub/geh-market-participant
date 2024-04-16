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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public class GetPermissionRelationsHandlerTests
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

        // Act
        var permissionRelationHandler = new GetPermissionRelationsHandler(new PermissionRelationService(mockPermissionRepository.Object, mockUserRoleRepository.Object));
        var resultStream = await permissionRelationHandler.Handle(new GetPermissionRelationsCommand(), default);

        // Assert
        Assert.NotNull(resultStream);
        using var sr = new StreamReader(resultStream);
        var header = await sr.ReadLineAsync();
        var lines = new List<string>();
        while (!sr.EndOfStream)
        {
            var line = await sr.ReadLineAsync();
            Assert.NotNull(line);
            lines.Add(line);
        }

        Assert.Equal("PermissionName;MarketRole;UserRole", header);

        var filteredLines = lines.Where(e =>
            !e.Contains("GridAccessProvider", StringComparison.InvariantCulture) &&
            !e.Contains("BillingAgent", StringComparison.InvariantCulture));
        foreach (var line in filteredLines)
        {
            Assert.Contains(line, lines);
        }

        Assert.Contains("ActorsManage;BillingAgent;TestRoleBillingAgent", lines);
        Assert.Contains("CalculationsManage;BillingAgent;TestRoleBillingAgent", lines);
        Assert.Contains("ActorsManage;GridAccessProvider;TestRoleGridAccessProvider", lines);
        Assert.Contains("CalculationsManage;GridAccessProvider;TestRoleGridAccessProvider", lines);
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
