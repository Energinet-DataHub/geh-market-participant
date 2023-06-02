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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Handlers.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class InitiateMitIdSignupHandlerTests
{
    [Fact]
    public async Task Handle_UserFound_InitiatesMitIdSignup()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userId = Guid.NewGuid();
        var user = new User(
            new UserId(userId),
            new ExternalUserId(Guid.NewGuid()),
            Enumerable.Empty<UserRoleAssignment>(),
            null,
            null);

        userRepositoryMock.Setup(x => x.GetAsync(new UserId(userId)))
            .ReturnsAsync(user);

        var target = new InitiateMitIdSignupHandler(userRepositoryMock.Object);

        var inviteUserCommand = new InitiateMitIdSignupCommand(userId);

        // act
        await target.Handle(inviteUserCommand, default);

        // assert
        userRepositoryMock
            .Verify(x => x.AddOrUpdateAsync(It.Is<User>(u => u == user && u.MitIdSignupInitiatedAt > DateTimeOffset.UtcNow.AddMinutes(-1))));
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userId = Guid.NewGuid();

        userRepositoryMock.Setup(x => x.GetAsync(new UserId(userId)))
            .ReturnsAsync((User?)null);

        var target = new InitiateMitIdSignupHandler(userRepositoryMock.Object);

        var inviteUserCommand = new InitiateMitIdSignupCommand(userId);

        // act + assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(inviteUserCommand, default));
    }
}
