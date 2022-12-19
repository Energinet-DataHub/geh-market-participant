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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetUserRolesIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserRolesIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserRoleTemplates_NoTemplates_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var command = new GetUserRolesCommand(actorId, userId);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Empty(response.Roles);
    }

    [Fact]
    public async Task GetUserRoleTemplates_HasTwoTemplates_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.OrganizationView });

        var command = new GetUserRolesCommand(actorId, userId);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Equal(2, response.Roles.Count());
    }

    [Fact]
    public async Task GetUserRoleTemplates_HasTwoActors_ReturnsTemplateFromEach()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId1, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var (actorId2, _, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId1, userId, new[] { Permission.UsersManage });

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId2, userId, new[] { Permission.OrganizationView });

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId2, userId, new[] { Permission.GridAreasManage });

        var command1 = new GetUserRolesCommand(actorId1, userId);
        var command2 = new GetUserRolesCommand(actorId2, userId);

        // Act
        var response1 = await mediator.Send(command1);
        var response2 = await mediator.Send(command2);

        // Assert
        Assert.Single(response1.Roles);
        Assert.Equal(2, response2.Roles.Count());
    }
}
