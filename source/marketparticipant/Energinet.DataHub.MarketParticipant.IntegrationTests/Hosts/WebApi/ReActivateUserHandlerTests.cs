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
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ReActivateUserHandlerTests(
    MarketParticipantDatabaseFixture databaseFixture,
    GraphServiceClientFixture graphServiceClientFixture)
    : WebApiIntegrationTestsBase<Startup>(databaseFixture)
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture = databaseFixture;

    [Fact]
    public async Task ReActivate_UserWithStatusInactive_StatusBecomesActive()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var userChangeIdentity = await _databaseFixture.PrepareUserAsync();
        var userContext = new Mock<IUserContext<FrontendUser>>();
        userContext
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(userChangeIdentity.Id, Guid.NewGuid(), Guid.NewGuid(), true));
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var testEmail = new RandomlyGeneratedEmailAddress();
        var externalUserId = await graphServiceClientFixture.CreateUserAsync(testEmail);

        var identityRepository = scope.ServiceProvider.GetRequiredService<IUserIdentityRepository>();

        var userIdentityInit = await identityRepository.GetAsync(externalUserId);
        var user = await _databaseFixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(e => e.ExternalId = externalUserId.Value));

        // Act
        var command = new ReActivateUserCommand(user.Id);
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(command);

        // Assert
        var userIdentity = await identityRepository.GetAsync(externalUserId);
        Assert.NotNull(userIdentityInit);
        Assert.NotNull(userIdentity);
        Assert.Equal(UserIdentityStatus.Inactive, userIdentityInit.Status);
        Assert.Equal(UserIdentityStatus.Active, userIdentity.Status);
    }

    [Fact]
    public async Task ReActivate_IfNotInactive_StopFlow()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        var userEntity = await SetupUserEntityAndMocks(host, userIdentityRepositoryMock, UserIdentityStatus.Active);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new ReActivateUserCommand(userEntity.Id);

        // Act
        await mediator.Send(command);

        // Assert
        userIdentityRepositoryMock.Verify(x => x.EnableUserAccountAsync(It.IsAny<ExternalUserId>()), Times.Never);
    }

    [Fact]
    public async Task ReActivate_AuditLogCreated()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        var userEntity = await SetupUserEntityAndMocks(host, userIdentityRepositoryMock, UserIdentityStatus.Inactive);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new ReActivateUserCommand(userEntity.Id);

        // Act
        await mediator.Send(command);

        // Assert
        await context
            .UserIdentityAuditLogEntries
            .SingleAsync(log =>
                log.UserId == userEntity.Id &&
                log.Field == (int)UserIdentityAuditLogField.Status &&
                log.NewValue == UserStatus.Active.ToString());
    }

    private async Task<UserEntity> SetupUserEntityAndMocks(WebApiIntegrationTestHost host, Mock<IUserIdentityRepository> userIdentityRepositoryMock, UserIdentityStatus status)
    {
        var userContext = new Mock<IUserContext<FrontendUser>>();
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new RandomlyGeneratedEmailAddress(),
            status,
            "first",
            "last",
            null,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        var userEntity = await _databaseFixture.PrepareUserAsync();

        userContext
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(userEntity.Id, Guid.NewGuid(), Guid.NewGuid(), true));

        userIdentityRepositoryMock
            .Setup(x => x.GetAsync(new ExternalUserId(userEntity.ExternalId)))
            .ReturnsAsync(userIdentity);
        return userEntity;
    }
}
