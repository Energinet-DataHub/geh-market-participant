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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UpdateUserRoleIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateUserRoleIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserRoleName()
    {
        // Create context user
        var (_, frontendUserId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        scope.Container.MockFrontendUser(frontendUserId);

        var mediator = scope.GetInstance<IMediator>();

        var (_, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var userRoleNameToUpdate = "UpdateUserRoleName";
        var newName = "UpdateUserRoleName updated";

        var userRoleId = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync(userRoleNameToUpdate, "Description", UserRoleStatus.Active, EicFunction.BillingAgent, new[] { Permission.UsersView });

        var updateCommand = new UpdateUserRoleCommand(
            userId,
            userRoleId.Value,
            new UpdateUserRoleDto(newName, "Description", UserRoleStatus.Active, new Collection<int> { (int)Permission.UsersView }));

        var getUserRoleCommand = new GetUserRoleCommand(userRoleId.Value);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getUserRoleCommand);

        // Assert
        Assert.Equal(newName, response.Role.Name);
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserRoleDescription()
    {
        // Create context user
        var (_, frontendUserId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        scope.Container.MockFrontendUser(frontendUserId);

        var mediator = scope.GetInstance<IMediator>();

        var (_, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var userRoleDescriptionToUpdate = "UserRoleName";
        var newDescription = "UserRoleDescription updated";

        var userRoleId = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync("UpdateUserRoleDescription", userRoleDescriptionToUpdate, UserRoleStatus.Active, EicFunction.BillingAgent, new[] { Permission.UsersView });

        var updateCommand = new UpdateUserRoleCommand(
            userId,
            userRoleId.Value,
            new UpdateUserRoleDto("UpdateUserRoleDescription", newDescription, UserRoleStatus.Active, new Collection<int> { (int)Permission.UsersView }));

        var getUserRoleCommand = new GetUserRoleCommand(userRoleId.Value);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getUserRoleCommand);

        // Assert
        Assert.Equal(newDescription, response.Role.Description);
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserRoleStatus()
    {
        // Create context user
        var (_, frontendUserId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        scope.Container.MockFrontendUser(frontendUserId);

        var mediator = scope.GetInstance<IMediator>();

        var (_, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var userRoleStatusToUpdate = UserRoleStatus.Active;
        var newUserRoleStatus = UserRoleStatus.Inactive;

        var userRoleId = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync("UpdateUserRoleStatus", string.Empty, userRoleStatusToUpdate, EicFunction.BillingAgent, new[] { Permission.UsersView });

        var updateCommand = new UpdateUserRoleCommand(
            userId,
            userRoleId.Value,
            new UpdateUserRoleDto("UpdateUserRoleStatus", string.Empty, newUserRoleStatus, new Collection<int> { (int)Permission.UsersView }));

        var getUserRoleCommand = new GetUserRoleCommand(userRoleId.Value);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getUserRoleCommand);

        // Assert
        Assert.Equal(newUserRoleStatus, response.Role.Status);
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserPermissionsStatus()
    {
        // Create context user
        var (_, frontendUserId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        scope.Container.MockFrontendUser(frontendUserId);

        var mediator = scope.GetInstance<IMediator>();

        var (_, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var userRolePermissionsToUpdate = new[] { Permission.UsersView };
        var newUserRolePermissions = new Collection<int> { (int)Permission.UsersView, (int)Permission.UsersManage };

        var userRoleId = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync("UpdateUserPermissionsStatus", string.Empty, UserRoleStatus.Active, EicFunction.BillingAgent, userRolePermissionsToUpdate);

        var updateCommand = new UpdateUserRoleCommand(
            userId,
            userRoleId.Value,
            new UpdateUserRoleDto("UpdateUserPermissionsStatus", string.Empty, UserRoleStatus.Active, newUserRolePermissions));

        var getUserRoleCommand = new GetUserRoleCommand(userRoleId.Value);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getUserRoleCommand);

        // Assert
        var actualPermissionListAsInts = response.Role.Permissions.ToList();
        Assert.Equal(2, actualPermissionListAsInts.Count);
        Assert.Equivalent(newUserRolePermissions, actualPermissionListAsInts);
    }
}
