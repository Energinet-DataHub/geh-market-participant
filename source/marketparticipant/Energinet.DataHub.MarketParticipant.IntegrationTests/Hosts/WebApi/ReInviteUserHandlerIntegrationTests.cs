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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ReInviteUserHandlerIntegrationTests : IAsyncLifetime
{
    private const string TestUserEmail = "re-invitation-integration-test@datahub.dk";

    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public ReInviteUserHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture,
        GraphServiceClientFixture graphServiceClientFixture)
    {
        _databaseFixture = databaseFixture;
        _graphServiceClientFixture = graphServiceClientFixture;
    }

    [Fact]
    public async Task ReInviteUser_ExpiredInvitation_UserInvitedAgain()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var actor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization.Patch(t => t.Domain = "a.datahub.dk"),
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.DataHubAdministrator));

        var externalUserId = await _graphServiceClientFixture.CreateUserAsync(TestUserEmail);
        var targetUserEntity = TestPreparationEntities.UnconnectedUser.Patch(u =>
        {
            u.AdministratedByActorId = actor.Id;
            u.Email = $"{Guid.NewGuid()}@a.datahub.dk";
            u.ExternalId = externalUserId.Value;
            u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        });

        var invitedByUserEntity = TestPreparationEntities.UnconnectedUser.Patch(u => u.Email = $"{Guid.NewGuid()}@a.datahub.dk");
        var invitedByUser = await _databaseFixture.PrepareUserAsync(invitedByUserEntity);
        var targetUser = await _databaseFixture.PrepareUserAsync(targetUserEntity);

        var command = new ReInviteUserCommand(targetUser.Id, invitedByUser.Id);

        // Act
        await mediator.Send(command);

        // Assert
        var userRepository = scope.GetInstance<IUserRepository>();
        var createdUser = await userRepository.GetAsync(new UserId(targetUser.Id));
        Assert.NotNull(createdUser);
        Assert.True(createdUser.InvitationExpiresAt > DateTime.UtcNow);

        var userInviteAuditLogEntryRepository = scope.GetInstance<IUserInviteAuditLogEntryRepository>();
        var userInviteAuditLog = await userInviteAuditLogEntryRepository.GetAsync(createdUser.Id);
        Assert.Single(userInviteAuditLog, e => e.UserId == createdUser.Id);
    }

    public Task InitializeAsync() => _graphServiceClientFixture.CleanupExternalUserAsync(TestUserEmail);
    public Task DisposeAsync() => _graphServiceClientFixture.CleanupExternalUserAsync(TestUserEmail);
}
