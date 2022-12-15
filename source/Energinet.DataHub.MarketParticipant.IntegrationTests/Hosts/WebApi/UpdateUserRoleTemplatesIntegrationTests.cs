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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Moq;
using SimpleInjector;
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
        // Create context user
        var (actorIdContext, userIdContext, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mockUser = new FrontendUser(userIdContext, Guid.NewGuid(), actorIdContext, true);
        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        scope.Container.Register(() => userIdProvider.Object, Lifestyle.Singleton);
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplateAsync();

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
        Assert.Contains(response.Templates, x => x.Id == templateId.Value);
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddToExistingTemplates_ReturnsBoth()
    {
        // Create context user
        var (actorIdContext, userIdContext, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mockUser = new FrontendUser(userIdContext, Guid.NewGuid(), actorIdContext, true);
        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        scope.Container.Register(() => userIdProvider.Object, Lifestyle.Singleton);
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
        Assert.Contains(response.Templates, x => x.Id == templateId.Value);
        Assert.Contains(response.Templates, x => x.Id == roleTemplateId);
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddToUserWithMultipleActorsAndExistingTemplates_ReturnsCorrectForBothActors()
    {
        // Create context user
        var (actorIdContext, userIdContext, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mockUser = new FrontendUser(userIdContext, Guid.NewGuid(), actorIdContext, true);
        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        scope.Container.Register(() => userIdProvider.Object, Lifestyle.Singleton);
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

        var updateDto = new UpdateUserRoleAssignmentsDto(
            actorId,
            new List<UserRoleTemplateId> { templateId, new(roleTemplateId) });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(userId, updateDto);
        var getCommand = new GetUserRoleTemplatesCommand(actorId, userId);
        var getCommand2 = new GetUserRoleTemplatesCommand(actor2Id, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);
        var response2 = await mediator.Send(getCommand2);

        // Assert
        Assert.NotEmpty(response.Templates);
        Assert.Equal(2, response.Templates.Count());
        Assert.Single(response2.Templates);
        Assert.Contains(response.Templates, x => x.Id == roleTemplateId);
        Assert.Contains(response.Templates, x => x.Id == templateId.Value);
        Assert.Contains(response2.Templates, x => x.Id == roleTemplate2Id);
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddNewTemplateToEmptyCollection_TwoAuditLogsAdded()
    {
        // Create context user
        var (actorIdContext, userIdContext, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mockUser = new FrontendUser(userIdContext, Guid.NewGuid(), actorIdContext, true);
        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        scope.Container.Register(() => userIdProvider.Object, Lifestyle.Singleton);
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture.DatabaseManager.CreateUserAsync().ConfigureAwait(false);
        var templateId1 = await _fixture.DatabaseManager.CreateRoleTemplateAsync().ConfigureAwait(false);
        var templateId2 = await _fixture.DatabaseManager.CreateRoleTemplateAsync().ConfigureAwait(false);
        var updateDto = new UpdateUserRoleAssignmentsDto(actorId, new List<UserRoleTemplateId> { templateId1, templateId2 });

        var updateCommand = new UpdateUserRoleAssignmentsCommand(userId, updateDto);
        var getCommand = new GetUserRoleAssignmentAuditLogsCommand(updateCommand.UserId, updateCommand.RoleAssignmentsDto.ActorId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        var responseLogs = response.UserRoleAssignmentAuditLogs.ToList();
        responseLogs.Should().HaveCount(2);
        responseLogs.TrueForAll(e => e.AssignmentType == UserRoleAssignmentTypeAuditLog.Added).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserRoleTemplateAssignments_AddNewTemplateToEmptyCollection_ThreeAuditLogsAdded_OneRemoved()
    {
        // Create context user
        var (actorIdContext, userIdContext, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mockUser = new FrontendUser(userIdContext, Guid.NewGuid(), actorIdContext, true);
        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        scope.Container.Register(() => userIdProvider.Object, Lifestyle.Singleton);
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId, _) = await _fixture.DatabaseManager.CreateUserAsync().ConfigureAwait(false);
        var templateId1 = await _fixture.DatabaseManager.CreateRoleTemplateAsync().ConfigureAwait(false);
        var templateId2 = await _fixture.DatabaseManager.CreateRoleTemplateAsync().ConfigureAwait(false);
        var updateDto1 = new UpdateUserRoleAssignmentsDto(actorId, new List<UserRoleTemplateId> { templateId1, templateId2 });

        var templateId3 = await _fixture.DatabaseManager.CreateRoleTemplateAsync().ConfigureAwait(false);
        var updateDto2 = new UpdateUserRoleAssignmentsDto(actorId, new List<UserRoleTemplateId> { templateId2, templateId3 });

        var updateCommand1 = new UpdateUserRoleAssignmentsCommand(userId, updateDto1);
        var updateCommand2 = new UpdateUserRoleAssignmentsCommand(userId, updateDto2);
        var getCommand = new GetUserRoleAssignmentAuditLogsCommand(updateCommand1.UserId, updateCommand2.RoleAssignmentsDto.ActorId);

        // Act
        await mediator.Send(updateCommand1);
        await mediator.Send(updateCommand2);

        var response = await mediator.Send(getCommand);

        // Assert
        var responseLogs = response.UserRoleAssignmentAuditLogs.ToList();
        responseLogs.Should().HaveCount(4);

        var addedLogs = responseLogs.Where(l => l.AssignmentType == UserRoleAssignmentTypeAuditLog.Added);
        var removedLogs = responseLogs.Where(l => l.AssignmentType == UserRoleAssignmentTypeAuditLog.Removed);
        addedLogs.Should().HaveCount(3);
        removedLogs.Should().HaveCount(1);
    }
}
