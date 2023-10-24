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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Handlers.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class CheckEmailExistsHandlerTests
{
    [Fact]
    public async Task Called_WithEmailBelongingToUserInCallersOrganization_ReturnsTrue()
    {
        // arrange
        const string usersMailAddress = "johndoe@example.com";

        var organizationId = Guid.NewGuid();
        var target = SetupTarget(
            callersOrganization: organizationId,
            usersMailAddress: usersMailAddress,
            usersOrganization: organizationId);

        // act
        var actual = await target.Handle(new CheckEmailExistsCommand(usersMailAddress), default);

        // assert
        Assert.True(actual);
    }

    [Fact]
    public async Task Called_WithEmailBelongingToUserInCallerUnknownOrganization_ReturnsFalse()
    {
        // arrange
        const string usersMailAddress = "johndoe@example.com";

        var target = SetupTarget(
            callersOrganization: Guid.NewGuid(),
            usersMailAddress: usersMailAddress,
            usersOrganization: Guid.NewGuid());

        // act
        var actual = await target.Handle(new CheckEmailExistsCommand(usersMailAddress), default);

        // assert
        Assert.False(actual);
    }

    [Fact]
    public async Task Called_WithUnknownEmail_ReturnsFalse()
    {
        // arrange
        var organizationId = Guid.NewGuid();

        var target = SetupTarget(
            callersOrganization: organizationId,
            usersMailAddress: "johndoe@example.com",
            usersOrganization: organizationId);

        // act
        var actual = await target.Handle(new CheckEmailExistsCommand("janedoe@example.com"), default);

        // assert
        Assert.False(actual);
    }

    private static CheckEmailExistsHandler SetupTarget(Guid callersOrganization, string usersMailAddress, Guid usersOrganization)
    {
        var frontendUser = new FrontendUser(Guid.NewGuid(), callersOrganization, Guid.NewGuid(), false);

        var userContextMock = new Mock<IUserContext<FrontendUser>>();
        userContextMock.Setup(x => x.CurrentUser).Returns(frontendUser);

        var phoneNumber = new PhoneNumber("+45 12345678");

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new EmailAddress(usersMailAddress),
            UserIdentityStatus.Active,
            "First",
            "Last",
            phoneNumber,
            DateTimeOffset.Now,
            new SmsAuthenticationMethod(phoneNumber),
            Array.Empty<LoginIdentity>());

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        userIdentityRepositoryMock.Setup(x => x.GetAsync(userIdentity.Email))
            .ReturnsAsync(userIdentity);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new ActorId(frontendUser.ActorId),
            userIdentity.Id,
            Array.Empty<UserRoleAssignment>(),
            null,
            null);

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock.Setup(x => x.GetAsync(userIdentity.Id))
            .ReturnsAsync(user);

        var actor = new Actor(
            new ActorId(frontendUser.ActorId),
            new OrganizationId(usersOrganization),
            null,
            new MockedGln(),
            ActorStatus.Active,
            Array.Empty<ActorMarketRole>(),
            new ActorName("Power Plant 1"));

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock.Setup(x => x.GetAsync(actor.Id))
            .ReturnsAsync(actor);

        return new CheckEmailExistsHandler(
            userContextMock.Object,
            userIdentityRepositoryMock.Object,
            userRepositoryMock.Object,
            actorRepositoryMock.Object);
    }
}
