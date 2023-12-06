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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
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
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

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

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new DeactivateUserCommand(userEntity.Id, true);

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

    [Fact]
    public async Task Deactivate_UserIsNotAdministeredByIdentity_RolesAreRemovedForIdentityActor()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

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

        var userEntity = await _fixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(u => u.ExternalId = userIdentity.Id.Value));
        var organization = await _fixture.PrepareOrganizationAsync(TestPreparationEntities.ValidOrganization);
        var actor = await _fixture.PrepareActorAsync(organization, TestPreparationEntities.ValidActor.Patch(x => x.Status = ActorStatus.Active));
        var someOtherActor = await _fixture.PrepareActorAsync(organization, TestPreparationEntities.ValidActor.Patch(x => x.Status = ActorStatus.Active));
        var userRole = await _fixture.PrepareUserRoleAsync(EicFunction.EnergySupplier);

        await _fixture.AssignUserRoleAsync(userEntity.Id, actor.Id, userRole.Id);
        await _fixture.AssignUserRoleAsync(userEntity.Id, someOtherActor.Id, userRole.Id);

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        userIdentityRepositoryMock
            .Setup(x => x.GetAsync(new ExternalUserId(userEntity.ExternalId)))
            .ReturnsAsync(userIdentity);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        userContext
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(Guid.NewGuid(), organization.Id, actor.Id, false));

        var userBeforeAct = await ReloadUserAsync(userEntity);
        Assert.Equal(2, userBeforeAct.RoleAssignments.Count);
        Assert.Contains(userBeforeAct.RoleAssignments, x => x.ActorId == actor.Id);

        // act
        await using var scope = host.BeginScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new DeactivateUserCommand(userEntity.Id, false));
        var actual = await ReloadUserAsync(userBeforeAct);

        // assert
        Assert.Single(actual.RoleAssignments);
        Assert.Equal(someOtherActor.Id, actual.RoleAssignments.Single().ActorId);
    }

    private async Task<UserEntity> ReloadUserAsync(UserEntity userEntity)
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        return await context.Users.Include(x => x.RoleAssignments).SingleAsync(u => u.Id == userEntity.Id);
    }

    private async Task Deactivate_UserWithStatus_StatusBecomesInactive(
        UserIdentityStatus initialUserIdentityStatus,
        Func<ExternalUserId, UserEntity> preparedUserEntity)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepositoryMock.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

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

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new DeactivateUserCommand(userEntity.Id, true);

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

        var userStatusCalculator = scope.ServiceProvider.GetRequiredService<IUserStatusCalculator>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepository.GetAsync(new UserId(userEntity.Id));
        Assert.NotNull(user);

        var status = userStatusCalculator.CalculateUserStatus(user, userIdentityAfterDisable);
        Assert.Equal(UserStatus.Inactive, status);
    }
}
