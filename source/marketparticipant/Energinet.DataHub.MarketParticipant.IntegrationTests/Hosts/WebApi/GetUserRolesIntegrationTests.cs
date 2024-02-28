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
public sealed class GetUserRolesIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserRolesIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserRoles_NoUserRole_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();

        var command = new GetUserRolesCommand(actor.Id, user.Id);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Empty(response.Roles);
    }

    [Fact]
    public async Task GetUserRoles_HasTwoUserRoles_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();

        var user = await _fixture.PrepareUserAsync();
        var userRoleA = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        var userRoleB = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleA.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleB.Id);

        var command = new GetUserRolesCommand(actor.Id, user.Id);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Equal(2, response.Roles.Count());
    }

    [Fact]
    public async Task GetUserRoles_HasTwoActors_ReturnsUserRolesFromEach()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor1 = await _fixture.PrepareActorAsync();
        var actor2 = await _fixture.PrepareActorAsync();

        var user = await _fixture.PrepareUserAsync();
        var userRoleA = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        var userRoleB = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);
        var userRoleC = await _fixture.PrepareUserRoleAsync(PermissionId.GridAreasManage);

        await _fixture.AssignUserRoleAsync(user.Id, actor1.Id, userRoleA.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor2.Id, userRoleB.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor2.Id, userRoleC.Id);

        var command1 = new GetUserRolesCommand(actor1.Id, user.Id);
        var command2 = new GetUserRolesCommand(actor2.Id, user.Id);

        // Act
        var response1 = await mediator.Send(command1);
        var response2 = await mediator.Send(command2);

        // Assert
        Assert.Single(response1.Roles);
        Assert.Equal(2, response2.Roles.Count());
    }
}
