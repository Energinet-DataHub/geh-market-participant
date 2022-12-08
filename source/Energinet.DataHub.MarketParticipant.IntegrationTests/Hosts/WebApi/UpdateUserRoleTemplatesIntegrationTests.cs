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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UpdateUserRoleTemplatesIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateUserRoleTemplatesIntegrationTests(MarketParticipantDatabaseFixture fixture)
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

        var (actorId, userId) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplate();

        var updateDto = new UpdateUserRoleAssignmentsDto(
            actorId,
            new List<UserRoleTemplateId> { templateId });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(userId, updateDto);
        var getCommand = new GetUserRoleTemplatesCommand(actorId, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Templates);
        Assert.Equal("fake_value", response.Templates.First().Name);
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddToExistingTemplates_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var roleTemplateId = await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplate("fake_value_2", new[] { Permission.ActorManage });

        var updateDto = new UpdateUserRoleAssignmentsDto(
            actorId,
            new List<UserRoleTemplateId> { templateId, new(roleTemplateId) });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(userId, updateDto);
        var getCommand = new GetUserRoleTemplatesCommand(actorId, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Templates);
        Assert.Equal(2, response.Templates.Count());
        Assert.Contains(response.Templates, x => x.Name == "fake_value");
        Assert.Contains(response.Templates, x => x.Name == "fake_value_2");
    }
}
