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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UpdateUserRoleAssignmentsIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateUserRoleAssignmentsIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateUserRoleAssignments_AddNewRoleToEmptyCollection_ReturnsNewRole()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);
        host.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();

        var updates = new List<Guid> { userRole.Id };

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actor.Id, user.Id, new UpdateUserRoleAssignmentsDto(updates, Array.Empty<Guid>()));
        var getCommand = new GetUserRolesCommand(actor.Id, user.Id);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Roles);
        Assert.Contains(response.Roles, x => x.Id == userRole.Id);
    }

    [Fact]
    public async Task UpdateUserRoleAssignments_AddToExistingRoles_ReturnsBoth()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);
        host.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRoleA = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        var userRoleB = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRoleA.Id);

        var updates = new List<Guid> { userRoleB.Id, userRoleA.Id };

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actor.Id, user.Id, new UpdateUserRoleAssignmentsDto(updates, Array.Empty<Guid>()));
        var getCommand = new GetUserRolesCommand(actor.Id, user.Id);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Roles);
        Assert.Equal(2, response.Roles.Count());
        Assert.Contains(response.Roles, x => x.Id == userRoleB.Id);
        Assert.Contains(response.Roles, x => x.Id == userRoleA.Id);
    }

    [Fact]
    public async Task UpdateUserRoleAssignments_AddToUserWithMultipleActorsAndExistingRoles_ReturnsCorrectForBothActors()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);
        host.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor1 = await _fixture.PrepareActorAsync();
        var actor2 = await _fixture.PrepareActorAsync();

        var userRoleA = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        var userRoleB = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);
        var userRoleNew = await _fixture.PrepareUserRoleAsync(PermissionId.ActorsManage);

        var user = await _fixture.PrepareUserAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor1.Id, userRoleA.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor2.Id, userRoleB.Id);

        var updates = new List<Guid> { userRoleNew.Id, userRoleA.Id };

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actor1.Id, user.Id, new UpdateUserRoleAssignmentsDto(updates, Array.Empty<Guid>()));
        var getCommand = new GetUserRolesCommand(actor1.Id, user.Id);
        var getCommand2 = new GetUserRolesCommand(actor2.Id, user.Id);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);
        var response2 = await mediator.Send(getCommand2);

        // Assert
        Assert.NotEmpty(response.Roles);
        Assert.Equal(2, response.Roles.Count());
        Assert.Single(response2.Roles);
        Assert.Contains(response.Roles, x => x.Id == userRoleA.Id);
        Assert.Contains(response.Roles, x => x.Id == userRoleNew.Id);
        Assert.Contains(response2.Roles, x => x.Id == userRoleB.Id);
    }

    [Fact]
    public async Task UpdateUserRoleAssignments_AddNewUserRoleToEmptyCollection_TwoAuditLogsAdded()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);
        host.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole1 = await _fixture.PrepareUserRoleAsync();
        var userRole2 = await _fixture.PrepareUserRoleAsync();

        var updates = new List<Guid> { userRole1.Id, userRole2.Id };

        var updateCommand = new UpdateUserRoleAssignmentsCommand(actor.Id, user.Id, new UpdateUserRoleAssignmentsDto(updates, Array.Empty<Guid>()));
        var getCommand = new GetUserAuditLogsCommand(updateCommand.UserId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        var responseLogs = response.AuditLogs.ToList();
        Assert.Equal(2, responseLogs.Count);
        Assert.All(responseLogs, log => Assert.Equal(UserAuditedChange.UserRoleAssigned, log.Change));
    }

    [Fact]
    public async Task UpdateUserRoleAssignments_AddNewUserRoleToEmptyCollection_ThreeAuditLogsAdded_OneRemoved()
    {
        // Create context user
        var frontendUser1 = await _fixture.PrepareUserAsync();
        var frontendUser2 = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole1 = await _fixture.PrepareUserRoleAsync();
        var userRole2 = await _fixture.PrepareUserRoleAsync();
        var updates1 = new List<Guid> { userRole1.Id, userRole2.Id };

        var userRole3 = await _fixture.PrepareUserRoleAsync();
        var updates2A = new List<Guid> { userRole1.Id };
        var updates2B = new List<Guid> { userRole3.Id };

        var updateCommand1 = new UpdateUserRoleAssignmentsCommand(actor.Id, user.Id, new UpdateUserRoleAssignmentsDto(updates1, Array.Empty<Guid>()));
        var updateCommand2 = new UpdateUserRoleAssignmentsCommand(actor.Id, user.Id, new UpdateUserRoleAssignmentsDto(updates2B, updates2A));
        var getCommand = new GetUserAuditLogsCommand(updateCommand1.UserId);

        // Act
        await using var hostUpdate1 = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        hostUpdate1.ServiceCollection.MockFrontendUser(frontendUser1.Id);
        hostUpdate1.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scopeUpdate1 = hostUpdate1.BeginScope();
        var mediatorUpdate1 = scopeUpdate1.ServiceProvider.GetRequiredService<IMediator>();
        await mediatorUpdate1.Send(updateCommand1);

        await using var hostUpdate2 = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        hostUpdate2.ServiceCollection.MockFrontendUser(frontendUser2.Id);
        hostUpdate2.ServiceCollection.Replace(ServiceDescriptor.Scoped(_ => new Mock<IRequiredPermissionForUserRoleRuleService>().Object));

        await using var scopeUpdate2 = hostUpdate2.BeginScope();
        var mediatorUpdate2 = scopeUpdate2.ServiceProvider.GetRequiredService<IMediator>();
        await mediatorUpdate2.Send(updateCommand2);

        var response = await mediator.Send(getCommand);

        // Assert
        var responseLogs = response.AuditLogs.ToList();
        Assert.Equal(4, responseLogs.Count);

        var addedLogs = responseLogs.Where(l => l.Change == UserAuditedChange.UserRoleAssigned);
        var removedLogs = responseLogs.Where(l => l.Change == UserAuditedChange.UserRoleRemoved);
        Assert.Equal(3, addedLogs.Count());
        Assert.Equal(1, removedLogs.Count());
    }
}
