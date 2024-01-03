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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
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
public sealed class GetUserRoleAuditLogsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetUserRoleAuditLogsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task GetAuditLogs_Created_ReturnsSingleAudit()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var userRoleEntity = await _databaseFixture.PrepareUserRoleAsync();

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new GetUserRoleAuditLogsCommand(userRoleEntity.Id);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        var actorCreatedAudit = actual.AuditLogs.Single(log => log.Change == UserRoleAuditedChange.Status);
        Assert.Equal(UserRoleStatus.Active.ToString(), actorCreatedAudit.CurrentValue);
        Assert.True(actorCreatedAudit.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
        Assert.True(actorCreatedAudit.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public Task GetAuditLogs_ChangeName_IsAudited()
    {
        var expected = Guid.NewGuid().ToString();

        return TestAuditOfUserRoleChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => !log.IsInitialAssignment)
                    .Single(log => log.Change == UserRoleAuditedChange.Name);

                Assert.Equal(expected, expectedLog.CurrentValue);
            },
            userRole =>
            {
                userRole.Name = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeDescription_IsAudited()
    {
        var expected = Guid.NewGuid().ToString();

        return TestAuditOfUserRoleChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => !log.IsInitialAssignment)
                    .Single(log => log.Change == UserRoleAuditedChange.Description);

                Assert.Equal(expected, expectedLog.CurrentValue);
            },
            userRole =>
            {
                userRole.Description = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeStatus_IsAudited()
    {
        var expected = UserRoleStatus.Inactive;

        return TestAuditOfUserRoleChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => !log.IsInitialAssignment)
                    .Single(log => log.Change == UserRoleAuditedChange.Status);

                Assert.Equal(expected.ToString(), expectedLog.CurrentValue);
            },
            userRole =>
            {
                userRole.Status = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_AddedPermission_IsAudited()
    {
        var expected = PermissionId.UsersManage;

        return TestAuditOfUserRoleChangeAsync(
            response =>
            {
                Assert.Contains(response.AuditLogs, log => log.Change == UserRoleAuditedChange.PermissionAdded && expected == Enum.Parse<PermissionId>(log.CurrentValue!));
            },
            userRole =>
            {
                userRole.Permissions = new[] { expected };
            });
    }

    [Fact]
    public Task GetAuditLogs_RemovedPermission_IsAudited()
    {
        var other = PermissionId.UsersView;
        var expected = PermissionId.UsersManage;

        return TestAuditOfUserRoleChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => !log.IsInitialAssignment)
                    .Single(log => log.Change == UserRoleAuditedChange.PermissionRemoved);

                Assert.Equal(expected, Enum.Parse<PermissionId>(expectedLog.PreviousValue!));
            },
            userRole =>
            {
                userRole.Permissions = new[] { other, expected };
            },
            userRole =>
            {
                userRole.Permissions = new[] { other };
            });
    }

    private async Task TestAuditOfUserRoleChangeAsync(
        Action<GetUserRoleAuditLogsResponse> assert,
        params Action<UserRole>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();
        var userRoleEntity = await _databaseFixture.PrepareUserRoleAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var userRoleRepository = scope.ServiceProvider.GetRequiredService<IUserRoleRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetUserRoleAuditLogsCommand(userRoleEntity.Id);

        foreach (var action in changeActions)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var userRole = await userRoleRepository.GetAsync(new UserRoleId(userRoleEntity.Id));
            Assert.NotNull(userRole);

            action(userRole);
            await userRoleRepository.UpdateAsync(userRole);

            var auditLogs = await mediator.Send(command);

            foreach (var actorAuditLog in auditLogs.AuditLogs.Where(log => !log.IsInitialAssignment))
            {
                Assert.Equal(auditedUser.Id, actorAuditLog.AuditIdentityId);
                Assert.True(actorAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(actorAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));
            }
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);
    }
}
