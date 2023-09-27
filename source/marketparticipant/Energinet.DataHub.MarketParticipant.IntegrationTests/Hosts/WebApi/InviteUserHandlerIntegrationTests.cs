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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class InviteUserHandlerIntegrationTests : IAsyncLifetime
{
    private const string TestUserEmail = "invitation-integration-test@datahub.dk";

    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public InviteUserHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture,
        GraphServiceClientFixture graphServiceClientFixture)
    {
        _databaseFixture = databaseFixture;
        _graphServiceClientFixture = graphServiceClientFixture;
    }

    [Fact]
    public async Task InviteUser_ValidInvitation_UserCreated()
    {
        // Arrange
        var invitedByUserEntity = TestPreparationEntities.UnconnectedUser.Patch(u => u.Email = $"{Guid.NewGuid()}@datahub.dk");
        var invitedByUser = await _databaseFixture.PrepareUserAsync(invitedByUserEntity);

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        host.ServiceCollection.MockFrontendUser(invitedByUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization.Patch(t => t.Domain = "datahub.dk"),
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.DataHubAdministrator));

        var userRole = await _databaseFixture.PrepareUserRoleAsync(
            new[] { PermissionId.ActorsManage },
            EicFunction.DataHubAdministrator);

        var invitation = new UserInvitationDto(
            TestUserEmail,
            "Invitation Integration Tests",
            "(Always safe to delete)",
            "+45 70000000",
            actor.Id,
            new[] { userRole.Id });

        var command = new InviteUserCommand(invitation, invitedByUser.Id);

        // Act
        await mediator.Send(command);

        // Assert
        var createdExternalUser = await _graphServiceClientFixture.TryFindExternalUserAsync(TestUserEmail);
        Assert.NotNull(createdExternalUser);

        var createdExternalUserId = new ExternalUserId(createdExternalUser.Id!);

        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var createdUser = await userRepository.GetAsync(createdExternalUserId);
        Assert.NotNull(createdUser);
        Assert.True(createdUser.InvitationExpiresAt > DateTime.UtcNow);

        var userIdentityRepository = scope.ServiceProvider.GetRequiredService<IUserIdentityRepository>();
        var createdUserIdentity = await userIdentityRepository.GetAsync(createdExternalUserId);
        Assert.NotNull(createdUserIdentity);

        var userInviteAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IUserInviteAuditLogEntryRepository>();
        var userInviteAuditLog = await userInviteAuditLogEntryRepository.GetAsync(createdUser.Id);
        Assert.Single(userInviteAuditLog, e => e.UserId == createdUser.Id);

        var userRoleAssignmentAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IUserRoleAssignmentAuditLogEntryRepository>();
        var assignmentAuditLogEntries = await userRoleAssignmentAuditLogEntryRepository.GetAsync(createdUser.Id);
        Assert.Single(assignmentAuditLogEntries, e =>
            e.ChangedByUserId.Value == invitedByUser.Id &&
            e.AssignmentType == UserRoleAssignmentTypeAuditLog.Added &&
            e.ActorId.Value == actor.Id &&
            e.UserId == createdUser.Id);
    }

    public Task InitializeAsync() => _graphServiceClientFixture.CleanupExternalUserAsync(TestUserEmail);
    public Task DisposeAsync() => _graphServiceClientFixture.CleanupExternalUserAsync(TestUserEmail);
}
