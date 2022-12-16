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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UpdateUserRolesIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateUserRolesIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddNewTemplateToEmptyCollection_ReturnsNewTemplate()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplateAsync();

        var updateDto = new UpdateUserRolesDto(new List<UserRoleId> { templateId });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actorId, userId, updateDto);
        var getCommand = new GetUserRolesCommand(actorId, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Roles);
        Assert.Contains(response.Roles, x => x.Id == templateId.Value);
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddToExistingTemplates_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var roleTemplateId = await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplateAsync(new[] { Permission.ActorManage });

        var updateDto = new UpdateUserRolesDto(new List<UserRoleId> { templateId, new(roleTemplateId) });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actorId, userId, updateDto);
        var getCommand = new GetUserRolesCommand(actorId, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Roles);
        Assert.Equal(2, response.Roles.Count());
        Assert.Contains(response.Roles, x => x.Id == templateId.Value);
        Assert.Contains(response.Roles, x => x.Id == roleTemplateId);
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddToUserWithMultipleActorsAndExistingTemplates_ReturnsCorrectForBothActors()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, actor2Id, userId) = await _fixture
            .DatabaseManager
            .CreateUserWithTwoActorsAsync();

        var roleTemplateId = await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var roleTemplate2Id = await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actor2Id, userId, new[] { Permission.OrganizationManage });

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplateAsync(new[] { Permission.ActorManage });

        var updateDto = new UpdateUserRolesDto(new List<UserRoleId> { templateId, new(roleTemplateId) });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actorId, userId, updateDto);
        var getCommand = new GetUserRolesCommand(actorId, userId);
        var getCommand2 = new GetUserRolesCommand(actor2Id, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);
        var response2 = await mediator.Send(getCommand2);

        // Assert
        Assert.NotEmpty(response.Roles);
        Assert.Equal(2, response.Roles.Count());
        Assert.Single(response2.Roles);
        Assert.Contains(response.Roles, x => x.Id == roleTemplateId);
        Assert.Contains(response.Roles, x => x.Id == templateId.Value);
        Assert.Contains(response2.Roles, x => x.Id == roleTemplate2Id);
    }
}
