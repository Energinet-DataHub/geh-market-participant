﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetUserRolesToPermissionIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserRolesToPermissionIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserRolesToPermission_Found_ReturnsUserRole()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var userRole = await _fixture.PrepareUserRoleAsync();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetUserRolesToPermissionCommand((int)PermissionId.UsersView);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.NotNull(response.UserRoles);
        Assert.NotEmpty(response.UserRoles);
        Assert.Contains(userRole.Id, response.UserRoles.Select(x => x.Id));
    }

    [Fact]
    public async Task GetUserRole_NotFound_ReturnsEmpty()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new GetUserRolesToPermissionCommand((int)PermissionId.UserRolesManage);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.NotNull(response.UserRoles);
        Assert.Empty(response.UserRoles);
    }
}
