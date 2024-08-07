﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
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
public sealed class ResetUserTwoFactorAuthenticationHandlerTests : WebApiIntegrationTestsBase<MarketParticipantWebApiAssembly>
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ResetUserTwoFactorAuthenticationHandlerTests(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public Task Reset2Fa_StatusActive_StatusBecomesInvited()
    {
        return Reset2Fa_UserWithStatus_StatusBecomesInvited(UserIdentityStatus.Active, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
            }));
    }

    [Fact]
    public Task Reset2Fa_StatusInvited_StatusBecomesInvited()
    {
        return Reset2Fa_UserWithStatus_StatusBecomesInvited(UserIdentityStatus.Active, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
                u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1);
            }));
    }

    [Fact]
    public Task Reset2Fa_StatusInvitationExpiredBeforeUserDeactivated_StatusBecomesInvited()
    {
        return Reset2Fa_UserWithStatus_StatusBecomesInvited(UserIdentityStatus.Active, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
                u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
            }));
    }

    [Fact]
    public Task Reset2Fa_StatusInvitationExpiredAfterUserDeactivated_StatusBecomesInvited()
    {
        return Reset2Fa_UserWithStatus_StatusBecomesInvited(UserIdentityStatus.Inactive, externalId => TestPreparationEntities.UnconnectedUser.Patch(
            u =>
            {
                u.ExternalId = externalId.Value;
                u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
            }));
    }

    [Fact]
    public async Task Reset2Fa_AuditLogCreated()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        var userAuthenticationService = new Mock<IUserIdentityAuthenticationService>();
        host.ServiceCollection.RemoveAll<IUserIdentityAuthenticationService>();
        host.ServiceCollection.AddScoped(_ => userAuthenticationService.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new RandomlyGeneratedEmailAddress(),
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

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new ResetUserTwoFactorAuthenticationCommand(userEntity.Id);

        // Act
        await mediator.Send(command);

        // Assert
        await context
            .UserIdentityAuditLogEntries
            .SingleAsync(log =>
                log.UserId == userEntity.Id &&
                log.Field == (int)UserIdentityAuditLogField.Status &&
                log.NewValue == UserStatus.Invited.ToString());
    }

    private async Task Reset2Fa_UserWithStatus_StatusBecomesInvited(
        UserIdentityStatus initialUserIdentityStatus,
        Func<ExternalUserId, UserEntity> preparedUserEntity)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        var userAuthenticationService = new Mock<IUserIdentityAuthenticationService>();
        host.ServiceCollection.RemoveAll<IUserIdentityAuthenticationService>();
        host.ServiceCollection.AddScoped(_ => userAuthenticationService.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new RandomlyGeneratedEmailAddress(),
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

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new ResetUserTwoFactorAuthenticationCommand(userEntity.Id);

        // Act
        await mediator.Send(command);

        // Assert
        userAuthenticationService.Verify(x => x.RemoveAllSoftwareTwoFactorAuthenticationMethodsAsync(userIdentity.Id), Times.Once);

        var userIdentityAfter2FaReset = new UserIdentity(
            userIdentity.Id,
            userIdentity.Email,
            UserIdentityStatus.Active,
            userIdentity.FirstName,
            userIdentity.LastName,
            userIdentity.PhoneNumber,
            userIdentity.CreatedDate,
            userIdentity.Authentication,
            userIdentity.LoginIdentities);

        var userStatusCalculator = scope.ServiceProvider.GetRequiredService<IUserStatusCalculator>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepository.GetAsync(new UserId(userEntity.Id));
        Assert.NotNull(user);

        var status = userStatusCalculator.CalculateUserStatus(user, userIdentityAfter2FaReset);
        Assert.Equal(UserStatus.Invited, status);
    }
}
