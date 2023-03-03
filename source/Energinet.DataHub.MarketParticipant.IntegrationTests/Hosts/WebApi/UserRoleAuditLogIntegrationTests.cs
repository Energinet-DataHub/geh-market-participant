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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserRoleAuditLogIntegrationTest : WebApiIntegrationTestsBase, IAsyncLifetime
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserRoleAuditLogIntegrationTest(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_UserRole_AuditLogSaved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);

        var mediator = scope.GetInstance<IMediator>();

        var user = await _fixture.PrepareUserAsync();

        const string name = "Create_UserRole_AuditLogSaved";
        var createUserRoleDto = new CreateUserRoleDto(
            name,
            "description",
            UserRoleStatus.Active,
            EicFunction.BillingAgent,
            new Collection<int> { (int)Permission.ActorManage });

        var createUserRoleCommand = new CreateUserRoleCommand(user.Id, createUserRoleDto);
        var expectedResult = GenerateLogEntries(createUserRoleDto, Guid.NewGuid(), Guid.NewGuid()).First();

        // Act
        var response = await mediator.Send(createUserRoleCommand);
        var result = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(response.UserRoleId));

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList.Where(e => e.UserRoleId.Value == response.UserRoleId));
        Assert.Single(resultList.Where(e =>
            e.ChangeDescriptionJson.Equals(expectedResult.ChangeDescriptionJson, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task UpdateUserRole_AllChanges()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);

        var mediator = scope.GetInstance<IMediator>();

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();

        const string nameUpdate = "UpdateUserRole_NameChangedAuditLog_Updated";
        const string descriptionUpdate = "UpdateUserRole_DescriptionChangedAuditLog_Updated";
        const UserRoleStatus userRoleStatusUpdate = UserRoleStatus.Inactive;
        var userRolePermissionsUpdate = new Collection<int> { (int)Permission.UsersView, (int)Permission.UsersManage };

        var updateUserRoleDto = new UpdateUserRoleDto(
            nameUpdate,
            descriptionUpdate,
            userRoleStatusUpdate,
            userRolePermissionsUpdate);

        var updateUserRoleCommand = new UpdateUserRoleCommand(user.Id, userRole.Id, updateUserRoleDto);

        // Act
        await mediator.Send(updateUserRoleCommand);

        // Assert
        var result = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(userRole.Id));
        var resultList = result.ToList();
        Assert.Equal(4, resultList.Count);
        Assert.Single(resultList, e => e.UserRoleChangeType == UserRoleChangeType.NameChange);
        Assert.Single(resultList, e => e.UserRoleChangeType == UserRoleChangeType.DescriptionChange);
        Assert.Single(resultList, e => e.UserRoleChangeType == UserRoleChangeType.StatusChange);
        Assert.Single(resultList, e => e.UserRoleChangeType == UserRoleChangeType.PermissionsChange);
        Assert.DoesNotContain(resultList, e => e.UserRoleChangeType == UserRoleChangeType.EicFunctionChange);
    }

    [Fact]
    public async Task Get_UserRoleAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);

        var mediator = scope.GetInstance<IMediator>();

        var user = await _fixture.PrepareUserAsync();

        var createUserRoleDto1 = CreateUserRoleToSave("LogToGetAdded");
        var createUserRoleDto2 = CreateUserRoleToSave("LogToGet2");

        var createUserRoleCommand1 = new CreateUserRoleCommand(user.Id, createUserRoleDto1);
        var createUserRoleCommand2 = new CreateUserRoleCommand(user.Id, createUserRoleDto2);
        var expectedResult1 = GenerateLogEntries(createUserRoleDto1, Guid.NewGuid(), Guid.NewGuid()).First();
        var expectedResult2 = GenerateLogEntries(createUserRoleDto2, Guid.NewGuid(), Guid.NewGuid()).First();

        // Act
        var response1 = await mediator.Send(createUserRoleCommand1);
        var response2 = await mediator.Send(createUserRoleCommand2);

        var result1 = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(response1.UserRoleId));
        var result2 = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(response2.UserRoleId));

        // Assert
        var resultList1 = result1.ToList();
        var resultList2 = result2.ToList();
        Assert.Single(resultList1.Where(e =>
            e.ChangeDescriptionJson.Equals(expectedResult1.ChangeDescriptionJson, StringComparison.Ordinal)));
        Assert.Single(resultList2.Where(e =>
            e.ChangeDescriptionJson.Equals(expectedResult2.ChangeDescriptionJson, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Get_UserRoleAudit_Changed_Logs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);
        var userRoleRepository = new UserRoleRepository(context);

        var mediator = scope.GetInstance<IMediator>();

        var user = await _fixture.PrepareUserAsync();

        // Create user role with created audit log
        var createUserRoleDto = CreateUserRoleToSave("LogToGetAddedNew");
        var createUserRoleCommand = new CreateUserRoleCommand(user.Id, createUserRoleDto);
        var createResponse = await mediator.Send(createUserRoleCommand);

        // Create Change log entries
        var userRole = await userRoleRepository.GetAsync(new UserRoleId(createResponse.UserRoleId));
        var userRoleUpdate = new UserRole("LogToGetAddedNewUpdated", "descriptionUpdated", userRole!.Status, userRole.Permissions, userRole.EicFunction);
        var expectedUpdateResult = GenerateChangedLogEntries(userRole, userRoleUpdate, user.Id).ToList();
        await userRoleAuditLogEntryRepository.InsertAuditLogEntriesAsync(expectedUpdateResult);

        // Get audit logs
        var getAuditLogsCommand = new GetUserRoleAuditLogsCommand(createResponse.UserRoleId);

        // Act
        var getResponse = await mediator.Send(getAuditLogsCommand);

        // Assert
        var resultList = getResponse.UserRoleAuditLogs.ToList();
        var nameChange = resultList.FirstOrDefault(e => e.UserRoleChangeType == UserRoleChangeType.NameChange);
        var descriptionChangeChange = resultList.FirstOrDefault(e => e.UserRoleChangeType == UserRoleChangeType.DescriptionChange);

        Assert.True(resultList.Count == 3);
        Assert.Single(resultList.Where(e => e.UserRoleChangeType == UserRoleChangeType.Created));
        Assert.Single(resultList.Where(e => e.UserRoleChangeType == UserRoleChangeType.NameChange));
        Assert.Single(resultList.Where(e => e.UserRoleChangeType == UserRoleChangeType.DescriptionChange));
        Assert.Single(expectedUpdateResult, e => e.UserRoleChangeType == UserRoleChangeType.NameChange && e.ChangeDescriptionJson.Equals(nameChange!.ChangeDescriptionJson, StringComparison.Ordinal));
        Assert.Single(expectedUpdateResult, e => e.UserRoleChangeType == UserRoleChangeType.DescriptionChange && e.ChangeDescriptionJson.Equals(descriptionChangeChange!.ChangeDescriptionJson, StringComparison.Ordinal));
    }

    public async Task InitializeAsync()
    {
        // Add needed permissions with Eic functions for tests
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var allPermissionAuditLogs = await context.PermissionAuditLogEntries.ToListAsync();
        context.PermissionAuditLogEntries.RemoveRange(allPermissionAuditLogs);

        var allPermissions = await context.Permissions.ToListAsync();
        context.Permissions.RemoveRange(allPermissions);
        await context.SaveChangesAsync();

        var permissionToUseForTest = new PermissionEntity
        {
            Description = "Permission for test",
            Created = DateTimeOffset.UtcNow,
            Id = (int)Permission.ActorManage,
            EicFunctions =
            {
                new()
                {
                    EicFunction = EicFunction.BillingAgent,
                    PermissionId = (int)Permission.ActorManage
                }
            }
        };

        context.Permissions.Add(permissionToUseForTest);
        await context.SaveChangesAsync();
    }

    public new async Task DisposeAsync()
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var allPermissionAuditLogs = await context.PermissionAuditLogEntries.ToListAsync();
        context.PermissionAuditLogEntries.RemoveRange(allPermissionAuditLogs);
        var allPermissions = await context.Permissions.ToListAsync();
        context.Permissions.RemoveRange(allPermissions);
        await context.SaveChangesAsync();
    }

    private static CreateUserRoleDto CreateUserRoleToSave(string name)
    {
        return new CreateUserRoleDto(
            name,
            "description",
            UserRoleStatus.Active,
            EicFunction.BillingAgent,
            new Collection<int> { (int)Permission.ActorManage });
    }

    private static IEnumerable<UserRoleAuditLogEntry> GenerateLogEntries(CreateUserRoleDto createUserRoleDto, Guid? userId, Guid? userRoleId)
    {
        var userRoleAuditLogService = new UserRoleAuditLogService();

        var userRole = new UserRole(
            createUserRoleDto.Name,
            createUserRoleDto.Description,
            createUserRoleDto.Status,
            createUserRoleDto.Permissions.Select(x => (Permission)x),
            createUserRoleDto.EicFunction);

        return userRoleAuditLogService.BuildAuditLogsForUserRoleCreated(
            new UserId(userId ?? Guid.NewGuid()),
            new UserRoleId(userRoleId ?? Guid.NewGuid()),
            userRole);
    }

    private static IEnumerable<UserRoleAuditLogEntry> GenerateChangedLogEntries(UserRole current, UserRole updated, Guid? userId)
    {
        var userRoleAuditLogService = new UserRoleAuditLogService();

        return userRoleAuditLogService.BuildAuditLogsForUserRoleChanged(
            new UserId(userId ?? Guid.NewGuid()),
            current,
            updated);
    }
}
