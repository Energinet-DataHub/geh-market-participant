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
using Energinet.DataHub.MarketParticipant.Application.Handlers.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class ReInviteUserHandlerTests
{
    private static readonly Guid _validInvitedByUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_MissingUser_ThrowsException()
    {
        // Arrange
        var userInvitationServiceMock = new Mock<IUserInvitationService>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var target = new ReInviteUserHandler(
            userInvitationServiceMock.Object,
            userRepositoryMock.Object);

        var inviteUserCommand = new ReInviteUserCommand(Guid.Empty, _validInvitedByUserId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(inviteUserCommand, default));
    }

    [Fact]
    public async Task Handle_ExistingUser_SendsInvitationAgain()
    {
        // Arrange
        var userInvitationServiceMock = new Mock<IUserInvitationService>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var target = new ReInviteUserHandler(
            userInvitationServiceMock.Object,
            userRepositoryMock.Object);

        var userId = Guid.NewGuid();
        var mockedUser = TestPreparationModels.MockedUser(userId);

        userRepositoryMock
            .Setup(x => x.GetAsync(new UserId(userId)))
            .ReturnsAsync(mockedUser);

        var inviteUserCommand = new ReInviteUserCommand(userId, _validInvitedByUserId);

        // Act
        await target.Handle(inviteUserCommand, default);

        // Assert
        userInvitationServiceMock.Verify(x => x.ReInviteUserAsync(mockedUser, new UserId(_validInvitedByUserId)), Times.Once);
    }
}
