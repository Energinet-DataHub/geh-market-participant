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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class DeactivateUserHandlerTests : WebApiIntegrationTestsBase
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public DeactivateUserHandlerTests(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public Task Deactivate_StatusActive_StatusBecomesInactive()
    {
        return Deactivate_UserWithStatus_StatusBecomesInactive(UserIdentityStatus.Active, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
            }));
    }

    [Fact]
    public Task Deactivate_StatusInvited_StatusBecomesInactive()
    {
        return Deactivate_UserWithStatus_StatusBecomesInactive(UserIdentityStatus.Active, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
                u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1);
            }));
    }

    [Fact]
    public Task Deactivate_StatusInvitationExpiredBeforeUserDeactivated_StatusBecomesInactive()
    {
        return Deactivate_UserWithStatus_StatusBecomesInactive(UserIdentityStatus.Active, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
                u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
            }));
    }

    [Fact]
    public Task Deactivate_StatusInvitationExpiredAfterUserDeactivated_StatusBecomesInactive()
    {
        return Deactivate_UserWithStatus_StatusBecomesInactive(UserIdentityStatus.Inactive, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
                u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
            }));
    }

    [Fact]
    public async Task Deactivate_UserActive_AuditLogCreated()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        scope.Container!.Register(() => userIdentityRepositoryMock.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        scope.Container!.Register(() => userContext.Object);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new MockedEmailAddress(),
            UserIdentityStatus.Active,
            "first",
            "last",
            null,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        var userEntity = await _fixture.PrepareUserAsync();

        userContext
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(userEntity.Id, Guid.NewGuid(), Guid.NewGuid(), true));

        userIdentityRepositoryMock
            .Setup(x => x.GetAsync(new ExternalUserId(userEntity.ExternalId)))
            .ReturnsAsync(userIdentity);

        var mediator = scope.GetInstance<IMediator>();
        var command = new DeactivateUserCommand(userEntity.Id);

        // Act
        await mediator.Send(command);

        // Assert
        await context
            .UserIdentityAuditLogEntries
            .SingleAsync(log =>
                log.UserId == userEntity.Id &&
                log.Field == (int)UserIdentityAuditLogField.Status &&
                log.NewValue == UserStatus.Inactive.ToString());
    }

    private async Task Deactivate_UserWithStatus_StatusBecomesInactive(
        UserIdentityStatus initialUserIdentityStatus,
        Func<ExternalUserId, UserEntity> preparedUserEntity)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        scope.Container!.Register(() => userIdentityRepositoryMock.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        scope.Container!.Register(() => userContext.Object);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new MockedEmailAddress(),
            initialUserIdentityStatus,
            "first",
            "last",
            null,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        var userEntity = await _fixture.PrepareUserAsync(preparedUserEntity(userIdentity.Id));

        userContext
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(userEntity.Id, Guid.NewGuid(), Guid.NewGuid(), true));

        userIdentityRepositoryMock
            .Setup(x => x.GetAsync(new ExternalUserId(userEntity.ExternalId)))
            .ReturnsAsync(userIdentity);

        var mediator = scope.GetInstance<IMediator>();
        var command = new DeactivateUserCommand(userEntity.Id);

        // Act
        await mediator.Send(command);

        // Assert
        userIdentityRepositoryMock.Verify(x => x.DisableUserAccountAsync(userIdentity.Id), Times.Once);

        var userIdentityAfterDisable = new UserIdentity(
            userIdentity.Id,
            userIdentity.Email,
            UserIdentityStatus.Inactive,
            userIdentity.FirstName,
            userIdentity.LastName,
            userIdentity.PhoneNumber,
            userIdentity.CreatedDate,
            userIdentity.Authentication,
            userIdentity.LoginIdentities);

        var userStatusCalculator = scope.GetInstance<IUserStatusCalculator>();
        var userRepository = scope.GetInstance<IUserRepository>();

        var user = await userRepository.GetAsync(new UserId(userEntity.Id));
        Assert.NotNull(user);

        var status = userStatusCalculator.CalculateUserStatus(user, userIdentityAfterDisable);
        Assert.Equal(UserStatus.Inactive, status);
    }
}
