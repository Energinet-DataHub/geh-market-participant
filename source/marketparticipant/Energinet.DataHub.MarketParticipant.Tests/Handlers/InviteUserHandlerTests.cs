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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Handlers.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class InviteUserHandlerTests
{
    private static readonly UserInvitationDto _validInvitation = new(
        "fake@value",
        new InvitationUserDetailsDto(
            "fake_value",
            "fake_value",
            "+45 70000000"),
        Guid.NewGuid(),
        new[] { Guid.NewGuid() });

    private static readonly Guid _validInvitedByUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_MissingActor_ThrowsException()
    {
        // Arrange
        var userInvitationServiceMock = new Mock<IUserInvitationService>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var target = new InviteUserHandler(
            userInvitationServiceMock.Object,
            organizationRepositoryMock.Object,
            actorRepositoryMock.Object,
            userRoleRepositoryMock.Object);

        var inviteUserCommand = new InviteUserCommand(_validInvitation, _validInvitedByUserId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(inviteUserCommand, default));
    }

    [Fact]
    public async Task Handle_DeletedOrganization_ThrowsException()
    {
        // Arrange
        var organizationId = new OrganizationId(Guid.NewGuid());

        var userInvitationServiceMock = new Mock<IUserInvitationService>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();

        var organization = TestPreparationModels.MockedOrganization(organizationId.Value);
        organization.Status = OrganizationStatus.Deleted;

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(organizationId))
            .ReturnsAsync(organization);

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(new ActorId(_validInvitation.AssignedActor)))
            .ReturnsAsync(new Actor(
                new ActorId(_validInvitation.AssignedActor),
                organizationId,
                null,
                new MockedGln(),
                ActorStatus.Active,
                new[] { new ActorMarketRole(EicFunction.MeteredDataResponsible) },
                new ActorName("fake_value"),
                null));

        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var target = new InviteUserHandler(
            userInvitationServiceMock.Object,
            organizationRepositoryMock.Object,
            actorRepositoryMock.Object,
            userRoleRepositoryMock.Object);

        var inviteUserCommand = new InviteUserCommand(_validInvitation, _validInvitedByUserId);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.Handle(inviteUserCommand, default));
    }

    [Fact]
    public async Task Handle_MissingUserRole_ThrowsException()
    {
        // Arrange
        var organizationId = new OrganizationId(Guid.NewGuid());

        var userInvitationServiceMock = new Mock<IUserInvitationService>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(organizationId))
            .ReturnsAsync(TestPreparationModels.MockedOrganization(organizationId.Value));

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(new ActorId(_validInvitation.AssignedActor)))
            .ReturnsAsync(new Actor(
                new ActorId(_validInvitation.AssignedActor),
                organizationId,
                null,
                new MockedGln(),
                ActorStatus.Active,
                new[] { new ActorMarketRole(EicFunction.MeteredDataResponsible) },
                new ActorName("fake_value"),
                null));

        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var target = new InviteUserHandler(
            userInvitationServiceMock.Object,
            organizationRepositoryMock.Object,
            actorRepositoryMock.Object,
            userRoleRepositoryMock.Object);

        var inviteUserCommand = new InviteUserCommand(_validInvitation, _validInvitedByUserId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(inviteUserCommand, default));
    }

    [Fact]
    public async Task Handle_ValidInvitation_InvitesUser()
    {
        // Arrange
        var organizationId = new OrganizationId(Guid.NewGuid());
        var userRoleId = new UserRoleId(_validInvitation.AssignedRoles.Single());

        var userInvitationServiceMock = new Mock<IUserInvitationService>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(organizationId))
            .ReturnsAsync(TestPreparationModels.MockedOrganization(organizationId.Value));

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(new ActorId(_validInvitation.AssignedActor)))
            .ReturnsAsync(new Actor(
                new ActorId(_validInvitation.AssignedActor),
                organizationId,
                null,
                new MockedGln(),
                ActorStatus.Active,
                new[] { new ActorMarketRole(EicFunction.MeteredDataResponsible) },
                new ActorName("fake_value"),
                null));

        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        userRoleRepositoryMock
            .Setup(userRoleRepository => userRoleRepository.GetAsync(userRoleId))
            .ReturnsAsync(new UserRole(
                userRoleId,
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                Array.Empty<PermissionId>(),
                EicFunction.MeteredDataResponsible));

        var target = new InviteUserHandler(
            userInvitationServiceMock.Object,
            organizationRepositoryMock.Object,
            actorRepositoryMock.Object,
            userRoleRepositoryMock.Object);

        var inviteUserCommand = new InviteUserCommand(_validInvitation, _validInvitedByUserId);

        // Act
        await target.Handle(inviteUserCommand, default);

        // Assert
        userInvitationServiceMock.Verify(userInvitationService => userInvitationService.InviteUserAsync(
            It.Is<UserInvitation>(ui =>
                ui.Email.Address == _validInvitation.Email &&
                ui.InvitationUserDetails!.FirstName == _validInvitation.InvitationUserDetails!.FirstName &&
                ui.InvitationUserDetails!.LastName == _validInvitation.InvitationUserDetails!.LastName &&
                ui.InvitationUserDetails!.PhoneNumber.Number == _validInvitation.InvitationUserDetails!.PhoneNumber &&
                ui.AssignedActor.Id.Value == _validInvitation.AssignedActor &&
                ui.AssignedRoles.Single().Id.Value == _validInvitation.AssignedRoles.Single()),
            It.Is<UserId>(u => u.Value == _validInvitedByUserId)));
    }
}
