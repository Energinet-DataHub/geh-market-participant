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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
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
public sealed class GetUserAuditLogEntriesHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserAuditLogEntriesHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserAuditLogs_NoAssignmentAuditLogs_ReturnsEmptyLog()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var (_, userId, _) = await _fixture.DatabaseManager.CreateUserAsync();

        var mediator = scope.GetInstance<IMediator>();
        var command = new GetUserAuditLogsCommand(userId);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        Assert.Empty(actual.UserRoleAssignmentAuditLogs);
    }

    [Fact]
    public async Task GetUserAuditLogs_WithTwoAssignmentAuditLogs_ReturnsTheAuditLogs()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var (_, frontendUserId, frontendExternalUserId) = await _fixture.DatabaseManager.CreateUserAsync();
        scope.Container.MockFrontendUser(frontendExternalUserId);

        var userRoleId = await _fixture.DatabaseManager.CreateRoleTemplateAsync();
        var (actorId, userId, _) = await _fixture.DatabaseManager.CreateUserAsync();

        var mediator = scope.GetInstance<IMediator>();

        // Add some entries to the audit log.
        {
            await mediator.Send(new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(new[] { userRoleId.Value }, Array.Empty<Guid>())));

            await Task.Delay(1500); // Wait a bit for the timestamp to change.

            await mediator.Send(new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(Array.Empty<Guid>(), new[] { userRoleId.Value })));
        }

        var command = new GetUserAuditLogsCommand(userId);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        var assignmentAuditLogs = actual
            .UserRoleAssignmentAuditLogs
            .OrderBy(x => x.Timestamp)
            .ToList();

        Assert.Equal(2, assignmentAuditLogs.Count);
        Assert.True(assignmentAuditLogs[0].Timestamp < assignmentAuditLogs[1].Timestamp);

        Assert.Equal(UserRoleAssignmentTypeAuditLog.Added, assignmentAuditLogs[0].AssignmentType);
        Assert.Equal(actorId, assignmentAuditLogs[0].ActorId);
        Assert.Equal(userRoleId.Value, assignmentAuditLogs[0].UserRoleId);
        Assert.Equal(frontendUserId, assignmentAuditLogs[0].ChangedByUserId);

        Assert.Equal(UserRoleAssignmentTypeAuditLog.Removed, assignmentAuditLogs[1].AssignmentType);
        Assert.Equal(actorId, assignmentAuditLogs[1].ActorId);
        Assert.Equal(userRoleId.Value, assignmentAuditLogs[1].UserRoleId);
        Assert.Equal(frontendUserId, assignmentAuditLogs[1].ChangedByUserId);
    }
}
