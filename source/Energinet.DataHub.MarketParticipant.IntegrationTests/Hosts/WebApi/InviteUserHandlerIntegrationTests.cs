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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class InviteUserHandlerIntegrationTests : IClassFixture<GraphServiceClientFixture>, IAsyncLifetime
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
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var actor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.DataHubAdministrator));

        var userRole = await _databaseFixture.PrepareUserRoleAsync(
            new[] { Permission.ActorManage },
            EicFunction.DataHubAdministrator);

        var invitation = new UserInvitationDto(
            TestUserEmail,
            "Invitation Integration Tests",
            "(Always safe to delete)",
            "+45 70000000",
            actor.Id,
            new[] { userRole.Id });

        var command = new InviteUserCommand(invitation);

        // Act
        await mediator.Send(command);

        // Assert
        var createdExternalUser = await TryFindExternalUserAsync();
        Assert.NotNull(createdExternalUser);

        var createdExternalUserId = new ExternalUserId(createdExternalUser);

        var userRepository = scope.GetInstance<IUserRepository>();
        var createdUser = await userRepository.GetAsync(createdExternalUserId);
        Assert.NotNull(createdUser);

        var userIdentityRepository = scope.GetInstance<IUserIdentityRepository>();
        var createdUserIdentity = await userIdentityRepository.GetAsync(createdExternalUserId);
        Assert.NotNull(createdUserIdentity);
    }

    public Task InitializeAsync() => CleanupExternalUserAsync();
    public Task DisposeAsync() => CleanupExternalUserAsync();

    private async Task CleanupExternalUserAsync()
    {
        var existingUser = await TryFindExternalUserAsync();
        if (existingUser == null)
            return;

        await _graphServiceClientFixture.Client
            .Users[existingUser]
            .Request()
            .DeleteAsync();
    }

    private async Task<string?> TryFindExternalUserAsync()
    {
        var usersRequest = await _graphServiceClientFixture.Client
            .Users
            .Request()
            .Filter($"identities/any(id:id/issuer eq '{_graphServiceClientFixture.Issuer}' and id/issuerAssignedId eq '{TestUserEmail}')")
            .Select(user => user.Id)
            .GetAsync()
            .ConfigureAwait(false);

        var users = await usersRequest
            .IteratePagesAsync(_graphServiceClientFixture.Client)
            .ConfigureAwait(false);

        var user = users.SingleOrDefault();
        return user?.Id;
    }
}
