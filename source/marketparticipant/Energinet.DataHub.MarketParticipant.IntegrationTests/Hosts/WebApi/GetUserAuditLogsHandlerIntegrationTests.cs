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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
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
    private const string InitialFirstName = "initial_first_name";
    private const string InitialLastName = "initial_last_name";
    private const string InitialPhoneNumber = "+45 00000000";

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
                    .AuditLogs
                    .Single(log => log.Change == UserAuditedChange.UserRoleAssigned);

                Assert.Equal($"({assignedActor.Id};{assignedUserRole.Id})", expectedLog.CurrentValue);
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
                    .AuditLogs
                    .Single(log => log.Change == UserAuditedChange.UserRoleRemoved);

                Assert.Equal($"({assignedActor.Id};{assignedUserRole.Id})", expectedLog.PreviousValue);
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

    [Fact]
    public async Task GetAuditLogs_AddRemoveAddAssignments_IsAudited()
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
                    .AuditLogs
                    .Single(log => log.Change == UserAuditedChange.UserRoleRemoved);

                Assert.Equal($"({assignedActor.Id};{assignedUserRole.Id})", expectedLog.PreviousValue);
            },
            user =>
            {
                user.RoleAssignments.Add(expected);
            },
            user =>
            {
                user.RoleAssignments.Clear();
            },
            user =>
            {
                user.RoleAssignments.Add(expected);
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeUserIdentityFirstName_IsAudited()
    {
        var expectedFirstName = Guid.NewGuid().ToString();

        return TestAuditOfUserIdentityChangeAsync(
            response =>
            {
                var expectedFirstNameLog = response
                    .AuditLogs
                    .Single(log => log.Change == UserAuditedChange.FirstName);

                Assert.Single(response.AuditLogs);
                Assert.Equal(expectedFirstName, expectedFirstNameLog.CurrentValue);
                Assert.Equal(InitialFirstName, expectedFirstNameLog.PreviousValue);
            },
            () => new UserIdentityUpdateDto(expectedFirstName, InitialLastName, InitialPhoneNumber));
    }

    [Fact]
    public Task GetAuditLogs_ChangeUserIdentityLastName_IsAudited()
    {
        var expectedLastName = Guid.NewGuid().ToString();

        return TestAuditOfUserIdentityChangeAsync(
            response =>
            {
                var expectedLastNameLog = response
                    .AuditLogs
                    .Single(log => log.Change == UserAuditedChange.LastName);

                Assert.Single(response.AuditLogs);
                Assert.Equal(expectedLastName, expectedLastNameLog.CurrentValue);
                Assert.Equal("initial_last_name", expectedLastNameLog.PreviousValue);
            },
            () => new UserIdentityUpdateDto(InitialFirstName, expectedLastName, InitialPhoneNumber));
    }

    [Fact]
    public Task GetAuditLogs_ChangeUserIdentityPhoneNumber_IsAudited()
    {
        var expectedPhoneNumber = "+45 12345678";

        return TestAuditOfUserIdentityChangeAsync(
            response =>
            {
                var expectedPhoneNumberLog = response
                    .AuditLogs
                    .Single(log => log.Change == UserAuditedChange.PhoneNumber);

                Assert.Single(response.AuditLogs);
                Assert.Equal(expectedPhoneNumber, expectedPhoneNumberLog.CurrentValue);
                Assert.Equal("+45 00000000", expectedPhoneNumberLog.PreviousValue);
            },
            () => new UserIdentityUpdateDto(InitialFirstName, InitialLastName, expectedPhoneNumber));
    }

    private async Task TestAuditOfUserRoleAssignmentChangeAsync(
        Action<GetUserAuditLogsResponse> assert,
        params Action<User>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();
        var userEntity = await _databaseFixture.PrepareUserAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetUserAuditLogsCommand(userEntity.Id);
        var auditLogsProcessed = 0;

        foreach (var action in changeActions)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var user = await userRepository.GetAsync(new UserId(userEntity.Id));
            Assert.NotNull(user);

            action(user);
            await userRepository.AddOrUpdateAsync(user);

            var auditLogs = await mediator.Send(command);

            foreach (var userAuditLog in auditLogs.AuditLogs.Skip(auditLogsProcessed))
            {
                Assert.Equal(auditedUser.Id, userAuditLog.AuditIdentityId);
                Assert.True(userAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(userAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));

                auditLogsProcessed++;
            }
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);
    }

    private async Task TestAuditOfUserIdentityChangeAsync(
        Action<GetUserAuditLogsResponse> assert,
        params Func<UserIdentityUpdateDto>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();
        var userEntity = await _databaseFixture.PrepareUserAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        var userIdentityRepository = new Mock<IUserIdentityRepository>();

        userIdentityRepository
            .Setup(r => r.GetAsync(new ExternalUserId(userEntity.ExternalId)))
            .ReturnsAsync(new UserIdentity(
                new ExternalUserId(userEntity.ExternalId),
                new MockedEmailAddress(),
                UserIdentityStatus.Active,
                InitialFirstName,
                InitialLastName,
                new PhoneNumber(InitialPhoneNumber),
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined,
                Array.Empty<LoginIdentity>()));

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetUserAuditLogsCommand(userEntity.Id);
        var auditLogsProcessed = 0;

        foreach (var action in changeActions)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var updateUserIdentityCommand = new UpdateUserIdentityCommand(action(), userEntity.Id);
            await mediator.Send(updateUserIdentityCommand);

            var auditLogs = await mediator.Send(command);

            foreach (var userAuditLog in auditLogs.AuditLogs.Skip(auditLogsProcessed))
            {
                Assert.Equal(auditedUser.Id, userAuditLog.AuditIdentityId);
                Assert.True(userAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(userAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));

                auditLogsProcessed++;
            }
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);
    }
}
