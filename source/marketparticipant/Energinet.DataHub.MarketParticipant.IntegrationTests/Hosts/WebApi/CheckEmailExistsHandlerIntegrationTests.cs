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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
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
public sealed class CheckEmailExistsHandlerIntegrationTests(
    MarketParticipantDatabaseFixture databaseFixture,
    GraphServiceClientFixture graphServiceClientFixture)
{
    [Fact]
    public async Task EmailCheck_UserIdentityNotFound_ReturnsFalse()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);

        var testEmail = new RandomlyGeneratedEmailAddress();

        var command = new CheckEmailExistsCommand(testEmail);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.False(response);
    }

    [Fact]
    public async Task EmailCheck_UserNotFound_ReturnsFalse()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);

        var testEmail = new RandomlyGeneratedEmailAddress();

        var externalUserId = await graphServiceClientFixture.CreateUserAsync(testEmail);

        var command = new CheckEmailExistsCommand(testEmail);

        await using var scope = host.BeginScope();
        var identityRepository = scope.ServiceProvider.GetRequiredService<IUserIdentityRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        var externalUser = await identityRepository.GetAsync(externalUserId);

        // Assert
        Assert.NotNull(externalUser);
        Assert.False(response);
        Assert.Equal(externalUserId.Value, externalUser.Id.Value);
    }

    [Fact]
    public async Task EmailCheck_UserContextIsFas_ReturnsTrue()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);

        var testEmail = new RandomlyGeneratedEmailAddress();

        var externalUserId = await graphServiceClientFixture.CreateUserAsync(testEmail);

        await databaseFixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(e => e.ExternalId = externalUserId.Value));

        var command = new CheckEmailExistsCommand(testEmail);

        UpdateCurrentUserSetting(host.ServiceCollection, true, Guid.NewGuid());

        await using var scope = host.BeginScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        var user = await userRepository.GetAsync(externalUserId);

        // Assert
        Assert.NotNull(user);
        Assert.True(response);
        Assert.Equal(externalUserId.Value, user.ExternalId.Value);
    }

    [Fact]
    public async Task EmailCheck_CurrentUsersActorOrgNotEquals_ReturnsFalse()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);

        var testEmail = new RandomlyGeneratedEmailAddress();

        var externalUserId = await graphServiceClientFixture.CreateUserAsync(testEmail);

        var userEntity = await databaseFixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(e => e.ExternalId = externalUserId.Value));

        var command = new CheckEmailExistsCommand(testEmail);

        var frontendUser = UpdateCurrentUserSetting(host.ServiceCollection, false, Guid.NewGuid());

        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        // Assert
        var actor = await actorRepository.GetAsync(new ActorId(userEntity.AdministratedByActorId));

        Assert.NotNull(actor);
        Assert.False(actor.OrganizationId.Value == frontendUser.OrganizationId);
        Assert.False(response);
    }

    [Fact]
    public async Task EmailCheck_CurrentUserOrgAndActorEquals_ReturnsTrue()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);

        var testEmail = new RandomlyGeneratedEmailAddress();

        var externalUserId = await graphServiceClientFixture.CreateUserAsync(testEmail);

        var userEntity = await databaseFixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(e => e.ExternalId = externalUserId.Value));
        await using var context = databaseFixture.DatabaseManager.CreateDbContext();
        var actorForUser = await context.Actors.FindAsync(userEntity.AdministratedByActorId) ?? throw new InvalidOperationException("Actor not found");

        var command = new CheckEmailExistsCommand(testEmail);

        var frontendUser = UpdateCurrentUserSetting(host.ServiceCollection, false, actorForUser.OrganizationId);

        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        // Assert
        var actor = await actorRepository.GetAsync(new ActorId(userEntity.AdministratedByActorId));

        Assert.NotNull(actor);
        Assert.True(actor.OrganizationId.Value == frontendUser.OrganizationId);
        Assert.True(response);
    }

    private static FrontendUser UpdateCurrentUserSetting(IServiceCollection services, bool isFas, Guid orgId)
    {
        var mockUser = new FrontendUser(
            KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
            orgId,
            Guid.NewGuid(),
            isFas);

        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        services.Replace(ServiceDescriptor.Scoped<IUserContext<FrontendUser>>(_ => userIdProvider.Object));

        return mockUser;
    }
}
