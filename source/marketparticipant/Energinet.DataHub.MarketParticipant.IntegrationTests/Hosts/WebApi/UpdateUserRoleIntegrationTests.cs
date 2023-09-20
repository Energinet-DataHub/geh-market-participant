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

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
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
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var userRole = await _fixture.PrepareUserRoleAsync();
        var newName = "NewUserRoleNameTestsRunIntegration";

        var existingUserRoleWithSameNameInOtherMarketRoleScope = TestPreparationEntities.ValidUserRole.Patch(e => e.Name = newName);
        existingUserRoleWithSameNameInOtherMarketRoleScope.EicFunctions.Clear();
        existingUserRoleWithSameNameInOtherMarketRoleScope.EicFunctions.Add(new UserRoleEicFunctionEntity() { EicFunction = EicFunction.EnergySupplier });

        var updateCommand = new UpdateUserRoleCommand(
            frontendUser.Id,
            userRole.Id,
            new UpdateUserRoleDto(newName, "Description", UserRoleStatus.Active, new Collection<int> { (int)PermissionId.UsersView }));

        var getUserRoleCommand = new GetUserRoleCommand(userRole.Id);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getUserRoleCommand);

        // Assert
        Assert.Equal(newName, response.Role.Name);
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserRoleName_ButNameExistInMarketRoleScope_Throws()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await _fixture.PrepareUserRoleAsync(TestPreparationEntities.ValidUserRole.Patch(e => e.Name = "TestNameU1"));
        var userRoleToUpdate = await _fixture.PrepareUserRoleAsync(TestPreparationEntities.ValidUserRole.Patch(e => e.Name = "TestNameU2"));

        var newName = "TestNameU1";

        var updateCommand = new UpdateUserRoleCommand(
            frontendUser.Id,
            userRoleToUpdate.Id,
            new UpdateUserRoleDto(newName, "Description", UserRoleStatus.Active, new Collection<int> { (int)PermissionId.UsersView }));

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(updateCommand));
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserRoleDescription()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var userRole = await _fixture.PrepareUserRoleAsync();
        var newDescription = "UserRoleDescription updated";

        var updateCommand = new UpdateUserRoleCommand(
            frontendUser.Id,
            userRole.Id,
            new UpdateUserRoleDto("UpdateUserRoleDescription", newDescription, UserRoleStatus.Active, new Collection<int> { (int)PermissionId.UsersView }));

        var getUserRoleCommand = new GetUserRoleCommand(userRole.Id);

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
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var userRoleStatusToUpdate = UserRoleStatus.Active;
        var newUserRoleStatus = UserRoleStatus.Inactive;

        var userRole = await _fixture.PrepareUserRoleAsync(TestPreparationEntities.ValidUserRole.Patch(t => t.Status = userRoleStatusToUpdate));

        var updateCommand = new UpdateUserRoleCommand(
            frontendUser.Id,
            userRole.Id,
            new UpdateUserRoleDto("UpdateUserRoleStatus", string.Empty, newUserRoleStatus, new Collection<int> { (int)PermissionId.UsersView }));

        var getUserRoleCommand = new GetUserRoleCommand(userRole.Id);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getUserRoleCommand);

        // Assert
        Assert.Equal(newUserRoleStatus, response.Role.Status);
    }

    [Fact]
    public async Task UpdateUserRole_UpdateUserPermissions()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersView);
        var newUserRolePermissions = new Collection<int> { (int)PermissionId.UsersView, (int)PermissionId.UsersManage };

        var updateCommand = new UpdateUserRoleCommand(
            frontendUser.Id,
            userRole.Id,
            new UpdateUserRoleDto("UpdateUserPermissionsStatus", string.Empty, UserRoleStatus.Active, newUserRolePermissions));

        // Act
        await mediator.Send(updateCommand);

        // Assert
        var response = await mediator.Send(new GetUserRoleCommand(userRole.Id));

        var actualPermissionListAsInts = response.Role.Permissions.Select(x => x.Id).ToList();
        Assert.Equal(2, actualPermissionListAsInts.Count);
        Assert.Equivalent(newUserRolePermissions, actualPermissionListAsInts);
    }
}
