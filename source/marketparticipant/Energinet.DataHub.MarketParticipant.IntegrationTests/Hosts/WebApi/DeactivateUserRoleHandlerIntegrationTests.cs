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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class DeactivateUserRoleHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public DeactivateUserRoleHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeactivateUserRole_NoUsers_UserRoleIsInactive()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var userRole = await _fixture.PrepareUserRoleAsync();
        var deactivateUserCommand = new DeactivateUserRoleCommand(userRole.Id, frontendUser.Id);

        // Act
        await mediator.Send(deactivateUserCommand);

        // Assert
        var actualUserRole = await mediator.Send(new GetUserRoleCommand(userRole.Id));
        Assert.Equal(UserRoleStatus.Inactive, actualUserRole.Role.Status);
    }

    [Fact]
    public async Task DeactivateUserRole_HasUsers_UserRoleIsRemoved()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync();
        var userRoleA = await _fixture.PrepareUserRoleAsync();
        var userRoleB = await _fixture.PrepareUserRoleAsync();

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleA.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleB.Id);

        var deactivateUserCommand = new DeactivateUserRoleCommand(userRoleA.Id, frontendUser.Id);

        // Act
        await mediator.Send(deactivateUserCommand);

        // Assert
        var assignedRoles = await mediator.Send(new GetUserRolesCommand(actor.Id, user.Id));
        Assert.Single(assignedRoles.Roles);
        Assert.Single(assignedRoles.Roles, r => r.Id == userRoleB.Id);
    }

    [Fact]
    public async Task DeactivateUserRole_HasUsers_UserIsAudited()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var deactivateUserCommand = new DeactivateUserRoleCommand(userRole.Id, frontendUser.Id);

        // Act
        await mediator.Send(deactivateUserCommand);

        // Assert
        var auditLogs = await mediator.Send(new GetUserAuditLogsCommand(user.Id));
        Assert.Single(auditLogs.AuditLogs, r =>
            r.Change == UserAuditedChange.UserRoleRemovedDueToDeactivation &&
            r.PreviousValue == userRole.Id.ToString());
    }
}
