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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
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
public sealed class GetUserAuditLogsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetUserAuditLogsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task GetAuditLogs_AddUserRoleAssignment_IsAudited()
    {
        var assignedActor = await _databaseFixture.PrepareActorAsync();
        var assignedUserRole = await _databaseFixture.PrepareUserRoleAsync();

        var expected = new UserRoleAssignment(
            new ActorId(assignedActor.Id),
            new UserRoleId(assignedUserRole.Id));

        await TestAuditOfUserRoleAssignmentChangeAsync(
            response =>
            {
                var expectedLog = response
                    .UserRoleAssignmentAuditLogs
                    .Single(log => log.AssignmentType == UserRoleAssignmentTypeAuditLog.Added);

                Assert.Equal(assignedActor.Id, expectedLog.ActorId);
                Assert.Equal(assignedUserRole.Id, expectedLog.UserRoleId);
            },
            user =>
            {
                user.RoleAssignments.Add(expected);
            });
    }

    [Fact]
    public async Task GetAuditLogs_RemoveUserRoleAssignment_IsAudited()
    {
        var assignedActor = await _databaseFixture.PrepareActorAsync();
        var assignedUserRole = await _databaseFixture.PrepareUserRoleAsync();

        var expected = new UserRoleAssignment(
            new ActorId(assignedActor.Id),
            new UserRoleId(assignedUserRole.Id));

        await TestAuditOfUserRoleAssignmentChangeAsync(
            response =>
            {
                var expectedLog = response
                    .UserRoleAssignmentAuditLogs
                    .Single(log => log.AssignmentType == UserRoleAssignmentTypeAuditLog.Removed);

                Assert.Equal(assignedActor.Id, expectedLog.ActorId);
                Assert.Equal(assignedUserRole.Id, expectedLog.UserRoleId);
            },
            user =>
            {
                user.RoleAssignments.Add(expected);
            },
            user =>
            {
                user.RoleAssignments.Remove(expected);
            });
    }

    private async Task TestAuditOfUserRoleAssignmentChangeAsync(
        Action<GetUserAuditLogsResponse> assert,
        params Action<User>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();
        var auditedUser = await _databaseFixture.PrepareUserAsync();
        var userEntity = await _databaseFixture.PrepareUserAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();
        userContext
            .Setup(uc => uc.CurrentUser)
            .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        foreach (var action in changeActions)
        {
            var user = await userRepository.GetAsync(new UserId(userEntity.Id));
            Assert.NotNull(user);

            action(user);
            await userRepository.AddOrUpdateAsync(user);
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new GetUserAuditLogsCommand(userEntity.Id);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);

        // Skip initial audits.
        foreach (var userAuditLog in actual.UserRoleAssignmentAuditLogs)
        {
            Assert.Equal(auditedUser.Id, userAuditLog.AuditIdentityId);
            Assert.Equal(userEntity.Id, userAuditLog.UserId);
            Assert.True(userAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
            Assert.True(userAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));
        }
    }
}
