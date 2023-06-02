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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Moq;
using SimpleInjector;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.Organization;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserInvitationExpiredIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserInvitationExpiredIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UserInvitationExpiredCommand_NoUsersFoundToDisable()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var user1ExpiredSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1));
        var user1Expired = await _fixture.PrepareUserAsync(user1ExpiredSetup);
        var user2ExpiredSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(1));
        var user2Expired = await _fixture.PrepareUserAsync(user2ExpiredSetup);

        var userIdentityRepository = new Mock<IUserIdentityRepository>();

        scope.Container!.Register(() => userIdentityRepository.Object, Lifestyle.Scoped);

        var command = new UserInvitationExpiredCommand();

        // Act
        var mediator = scope.GetInstance<IMediator>();
        await mediator.Send(command);

        // Assert
        userIdentityRepository.Verify(x => x.DisableUserAccountAsync(new ExternalUserId(user1Expired.ExternalId)), Times.Never);
        userIdentityRepository.Verify(x => x.DisableUserAccountAsync(new ExternalUserId(user2Expired.ExternalId)), Times.Never);
    }

    [Fact]
    public async Task UserInvitationExpiredCommand_UsersAreExpextedToBeDisabled()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var user1ExpiredSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1));
        var user1Expired = await _fixture.PrepareUserAsync(user1ExpiredSetup);
        var user2ExpiredSetup = TestPreparationEntities.UnconnectedUser.Patch(u => u.InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1));
        var user2Expired = await _fixture.PrepareUserAsync(user2ExpiredSetup);

        var userIdentityRepository = new Mock<IUserIdentityRepository>();

        scope.Container!.Register(() => userIdentityRepository.Object, Lifestyle.Scoped);

        var command = new UserInvitationExpiredCommand();

        // Act
        var mediator = scope.GetInstance<IMediator>();
        await mediator.Send(command);

        // Assert
        userIdentityRepository.Verify(x => x.DisableUserAccountAsync(new ExternalUserId(user1Expired.ExternalId)), Times.Once);
        userIdentityRepository.Verify(x => x.DisableUserAccountAsync(new ExternalUserId(user2Expired.ExternalId)), Times.Once);
    }

    private static UserIdentity GetUserIdentityForTest(ExternalUserId externalUserId)
    {
        return new UserIdentity(
            externalUserId,
            new MockedEmailAddress(),
            UserStatus.Active,
            "firstName",
            "lastName",
            new PhoneNumber("23232323"),
            DateTimeOffset.Now,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());
    }
}
