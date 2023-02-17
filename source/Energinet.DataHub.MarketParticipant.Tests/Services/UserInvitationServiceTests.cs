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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class UserInvitationServiceTests
{
    private readonly UserInvitation _validInvitation = new(
        new EmailAddress("fake@value"),
        "John",
        "Doe",
        new PhoneNumber("00000000"),
        new SmsAuthenticationMethod(new PhoneNumber("+45 00000000")),
        new Actor(
            Guid.NewGuid(),
            null,
            new MockedGln(),
            ActorStatus.New,
            new[] { new ActorMarketRole(Guid.NewGuid(), EicFunction.BalanceResponsibleParty) },
            new ActorName("fake_value")),
        new[]
        {
            new UserRole(
                new UserRoleId(Guid.NewGuid()),
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                Array.Empty<Permission>(),
                EicFunction.BalanceResponsibleParty)
        });

    [Fact]
    public async Task InviteUserAsync_NoUser_CreatesAndSavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
        VerifyUserIdentityCreatedCorrectly(userIdentityRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_HasUserIdentityButNotLocalUser_CreatesAndSavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                new ExternalUserId(Guid.NewGuid()),
                _validInvitation.Email,
                UserStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
        VerifyUserIdentityCreatedCorrectly(userIdentityRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_HasUser_SavesPermissionsOnly()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                externalId,
                Array.Empty<UserRoleAssignment>()));

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                externalId,
                _validInvitation.Email,
                UserStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);

        userIdentityRepositoryMock.Verify(
            userIdentityRepository => userIdentityRepository.CreateAsync(It.IsAny<UserIdentity>()),
            Times.Never);
    }

    [Fact]
    public async Task InviteUserAsync_HasUserWithUserRoles_AddsNewUserRoles()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                externalId,
                new[] { new UserRoleAssignment(Guid.NewGuid(), new UserRoleId(Guid.NewGuid())) }));

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                externalId,
                _validInvitation.Email,
                UserStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation);

        // Assert
        var expectedRole = _validInvitation.AssignedRoles.Single();
        var expectedActor = _validInvitation.AssignedActor;
        var expectedAssignment = new UserRoleAssignment(expectedActor.Id, expectedRole.Id);

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.Is<User>(user =>
                user.RoleAssignments.Count == 2 &&
                user.RoleAssignments.Contains(expectedAssignment))),
            Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_HasUser_SendsInvitationEmail()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                externalId,
                new[] { new UserRoleAssignment(Guid.NewGuid(), new UserRoleId(Guid.NewGuid())) }));

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                externalId,
                _validInvitation.Email,
                UserStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation);

        // Assert
        emailEventRepositoryMock.Verify(emailEventRepository => emailEventRepository.InsertAsync(
            It.Is<EmailEvent>(emailEvent =>
                emailEvent.Email == _validInvitation.Email &&
                emailEvent.EmailEventType == EmailEventType.UserInvite)));
    }

    private void VerifyUserCreatedCorrectly(Mock<IUserRepository> userRepositoryMock)
    {
        var expectedRole = _validInvitation.AssignedRoles.Single();
        var expectedActor = _validInvitation.AssignedActor;
        var expectedAssignment = new UserRoleAssignment(expectedActor.Id, expectedRole.Id);

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.Is<User>(user => user.RoleAssignments.Single() == expectedAssignment)),
            Times.Once);
    }

    private void VerifyUserIdentityCreatedCorrectly(Mock<IUserIdentityRepository> userIdentityRepositoryMock)
    {
        userIdentityRepositoryMock.Verify(
            userIdentityRepository => userIdentityRepository.CreateAsync(It.Is<UserIdentity>(userIdentity =>
                userIdentity.Email == _validInvitation.Email &&
                userIdentity.PhoneNumber == _validInvitation.PhoneNumber &&
                userIdentity.FirstName == _validInvitation.FirstName &&
                userIdentity.LastName == _validInvitation.LastName)),
            Times.Once);
    }
}
