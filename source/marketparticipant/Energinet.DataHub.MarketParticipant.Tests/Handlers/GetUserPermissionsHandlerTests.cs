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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetUserPermissionsHandlerTests
{
    [Fact]
    public async Task Handle_2FaIsFalse_Retries()
    {
        // arrange
        var command = CreateCommand();
        var user = CreateUser(command);
        var userIdentityAuthenticationService = CreateUserIdentityAuthenticationService(user, noOfTimesToReturnFalse: 100);
        var target = CreateTarget(
            CreateUserRepository(command, user),
            CreateUserQueryRepository(user),
            userIdentityAuthenticationService);

        // act
        await target.Handle(command, default);

        // assert
        userIdentityAuthenticationService
            .Verify(x => x.HasTwoFactorAuthenticationAsync(user.ExternalId), Times.Exactly(4));
    }

    [Fact]
    public async Task Handle_2FaIsFalse_RetriesButShortcircuitsOnTrue()
    {
        // arrange
        var command = CreateCommand();
        var user = CreateUser(command);
        var userIdentityAuthenticationService = CreateUserIdentityAuthenticationService(user, noOfTimesToReturnFalse: 1);
        var target = CreateTarget(
            CreateUserRepository(command, user),
            CreateUserQueryRepository(user),
            userIdentityAuthenticationService);

        // act
        await target.Handle(command, default);

        // assert
        userIdentityAuthenticationService
            .Verify(x => x.HasTwoFactorAuthenticationAsync(user.ExternalId), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_2FaIsFalse_LeavesInviteExpiration()
    {
        // arrange
        var command = CreateCommand();
        var user = CreateUser(command);
        var target = CreateTarget(
            CreateUserRepository(command, user),
            CreateUserQueryRepository(user),
            CreateUserIdentityAuthenticationService(user, noOfTimesToReturnFalse: 100));

        // act
        await target.Handle(command, default);

        // assert
        Assert.NotNull(user.InvitationExpiresAt);
    }

    [Fact]
    public async Task Handle_2FaIsTrue_ClearsInviteExpiration()
    {
        // arrange
        var command = CreateCommand();
        var user = CreateUser(command);
        var target = CreateTarget(
            CreateUserRepository(command, user),
            CreateUserQueryRepository(user),
            CreateUserIdentityAuthenticationService(user, noOfTimesToReturnFalse: 0));

        // act
        await target.Handle(command, default);

        // assert
        Assert.Null(user.InvitationExpiresAt);
    }

    private static GetUserPermissionsCommand CreateCommand()
    {
        return new GetUserPermissionsCommand(Guid.NewGuid(), Guid.NewGuid());
    }

    private static User CreateUser(GetUserPermissionsCommand command)
    {
        return new User(
            new UserId(Guid.NewGuid()),
            new ActorId(command.ActorId),
            new ExternalUserId(command.ExternalUserId),
            [],
            null,
            DateTimeOffset.UtcNow.AddHours(24),
            null);
    }

    private static Mock<IUserRepository> CreateUserRepository(GetUserPermissionsCommand command, User user)
    {
        var userRepository = new Mock<IUserRepository>();

        userRepository
            .Setup(x => x.GetAsync(new ExternalUserId(command.ExternalUserId)))
            .ReturnsAsync(user);

        return userRepository;
    }

    private static Mock<IUserQueryRepository> CreateUserQueryRepository(User user)
    {
        var userQueryRepository = new Mock<IUserQueryRepository>();

        userQueryRepository
            .Setup(x => x.GetPermissionsAsync(user.AdministratedBy, user.ExternalId))
            .ReturnsAsync([]);

        userQueryRepository
            .Setup(x => x.IsFasAsync(user.AdministratedBy, user.ExternalId))
            .ReturnsAsync(false);

        return userQueryRepository;
    }

    private static Mock<IUserIdentityAuthenticationService> CreateUserIdentityAuthenticationService(User user, int noOfTimesToReturnFalse)
    {
        var userIdentityAuthenticationService = new Mock<IUserIdentityAuthenticationService>();

        var count = 0;

        userIdentityAuthenticationService
            .Setup(x => x.HasTwoFactorAuthenticationAsync(user.ExternalId))
            .ReturnsAsync((ExternalUserId _) => count++ == noOfTimesToReturnFalse);

        return userIdentityAuthenticationService;
    }

    private static GetUserPermissionsHandler CreateTarget(Mock<IUserRepository> userRepository, Mock<IUserQueryRepository> userQueryRepository, Mock<IUserIdentityAuthenticationService> userIdentityAuthenticationService)
    {
        return new GetUserPermissionsHandler(
            userRepository.Object,
            userQueryRepository.Object,
            new Mock<IUserIdentityOpenIdLinkService>().Object,
            userIdentityAuthenticationService.Object,
            new NullLogger<GetUserPermissionsHandler>());
    }
}
