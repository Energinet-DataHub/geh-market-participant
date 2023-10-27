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
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Energinet.DataHub.MarketParticipant.Tests.Handlers;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class UserInvitationServiceTests
{
    private readonly UserInvitation _validInvitation = new(
        new MockedEmailAddress(),
        "John",
        "Doe",
        new PhoneNumber("00000000"),
        new SmsAuthenticationMethod(new PhoneNumber("+45 00000000")),
        new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new[] { new ActorMarketRole(EicFunction.BalanceResponsibleParty) },
            new ActorName("fake_value"),
            new Collection<ActorCredentials>()),
        new[]
        {
            new UserRole(
                new UserRoleId(Guid.NewGuid()),
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                Array.Empty<PermissionId>(),
                EicFunction.BalanceResponsibleParty)
        });

    private readonly UserId _validInvitedByUserId = new UserId(Guid.NewGuid());

    [Fact]
    public async Task InviteUserAsync_NoUser_CreatesAndSavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
        VerifyUserIdentityCreatedCorrectly(userIdentityRepositoryMock);
        VerifyUserInvitationExpirationCorrectly(userRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_EmailNotAllowed_DoesNotCreateUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        organizationDomainValidationServiceMock
            .Setup(organizationDomainValidationService =>
                organizationDomainValidationService.ValidateUserEmailInsideOrganizationDomainsAsync(
                    It.IsAny<Actor>(),
                    _validInvitation.Email))
            .ThrowsAsync(new ValidationException());

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        var invitation = _validInvitation;

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.InviteUserAsync(invitation, _validInvitedByUserId));

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.IsAny<User>()),
            Times.Never);

        userIdentityRepositoryMock.Verify(
            userIdentityRepository => userIdentityRepository.CreateAsync(It.IsAny<UserIdentity>()),
            Times.Never);

        emailEventRepositoryMock.Verify(
            emailEventRepository => emailEventRepository.InsertAsync(It.IsAny<EmailEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task InviteUserAsync_HasUserIdentityButNotLocalUser_CreatesAndSavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                new ExternalUserId(Guid.NewGuid()),
                _validInvitation.Email,
                UserIdentityStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
        VerifyUserIdentityCreatedCorrectly(userIdentityRepositoryMock);
        VerifyUserInvitationExpirationCorrectly(userRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_HasUser_SavesPermissionsOnly()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                Array.Empty<UserRoleAssignment>(),
                null,
                null));

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                externalId,
                _validInvitation.Email,
                UserIdentityStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_HasUserWithUserRoles_AddsNewUserRoles()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                new[] { new UserRoleAssignment(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())) },
                null,
                null));

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                externalId,
                _validInvitation.Email,
                UserIdentityStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

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
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                new[] { new UserRoleAssignment(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())) },
                null,
                null));

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                externalId,
                _validInvitation.Email,
                UserIdentityStatus.Active,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        emailEventRepositoryMock.Verify(emailEventRepository => emailEventRepository.InsertAsync(
            It.Is<EmailEvent>(emailEvent =>
                emailEvent.Email == _validInvitation.Email &&
                emailEvent.EmailEventType == EmailEventType.UserInvite)));
    }

    [Fact]
    public async Task ReInviteUserAsync_NoUserIdentity_Throws()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var mockedUser = TestPreparationModels.MockedUser(Guid.NewGuid());

        userIdentityRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync((UserIdentity?)null);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        // act + assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() =>
            target.ReInviteUserAsync(mockedUser, _validInvitedByUserId));

        userIdentityRepositoryMock.Verify(e => e.GetAsync(mockedUser.ExternalId), Times.Once);
    }

    [Fact]
    public async Task ReInviteUserAsync_UserStatusNotInviteExpired_Throws()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var user = new User(
            new UserId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            new ExternalUserId(Guid.NewGuid()),
            new UserRoleAssignment[] { new(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())) },
            null,
            null);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new EmailAddress("test@test.dk"),
            UserIdentityStatus.Active,
            "FirstName",
            "LastName",
            new PhoneNumber("+45 12345678"),
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());

        userIdentityRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        // act + assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            target.ReInviteUserAsync(user, _validInvitedByUserId));
    }

    [Fact]
    public async Task ReInviteUserAsync_CompleteReInvite_Success()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogEntryRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogEntryRepository>();
        var userStatusCalculator = new UserStatusCalculator();

        var user = new User(
            new UserId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            new ExternalUserId(Guid.NewGuid()),
            new UserRoleAssignment[] { new(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())) },
            null,
            DateTimeOffset.UtcNow.AddDays(-1));

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new EmailAddress("test@test.dk"),
            UserIdentityStatus.Active,
            "FirstName",
            "LastName",
            new PhoneNumber("+45 12345678"),
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());

        userRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        userIdentityRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator);

        await target.ReInviteUserAsync(user, _validInvitedByUserId);

        // act + assert
        userIdentityRepositoryMock
            .Verify(u => u.EnableUserAccountAsync(userIdentity.Id), Times.Once);
        userRepositoryMock
            .Verify(u => u.AddOrUpdateAsync(user), Times.Once);
        emailEventRepositoryMock
            .Verify(e => e.InsertAsync(It.IsAny<EmailEvent>()), Times.Once);
        userInviteAuditLogEntryRepository
            .Verify(a => a.InsertAuditLogEntryAsync(It.IsAny<UserInviteAuditLogEntry>()), Times.Once);
    }

    private static void VerifyUserInvitationExpirationCorrectly(Mock<IUserRepository> userRepositoryMock)
    {
        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.Is<User>(user => user.InvitationExpiresAt != null && user.InvitationExpiresAt > DateTimeOffset.UtcNow)),
            Times.Once);
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
