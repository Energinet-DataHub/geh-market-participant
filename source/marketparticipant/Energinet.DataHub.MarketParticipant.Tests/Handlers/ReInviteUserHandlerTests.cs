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
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class ReInviteUserHandlerTests
{
    private static readonly Guid _validInvitedByUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ExistingUser_SendsInvitationAgain()
    {
        // Arrange
        var userInvitationServiceMock = new Mock<IUserInvitationService>();

        var target = new ReInviteUserHandler(userInvitationServiceMock.Object);

        var userId = Guid.NewGuid();

        var inviteUserCommand = new ReInviteUserCommand(userId, _validInvitedByUserId);

        // Act
        await target.Handle(inviteUserCommand, default);

        // Assert
        userInvitationServiceMock.Verify(x => x.ReInviteUserAsync(new UserId(userId), new UserId(_validInvitedByUserId)), Times.Once);
    }
}
