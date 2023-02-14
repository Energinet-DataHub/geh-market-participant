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
using Energinet.DataHub.MarketParticipant.Application.Commands;
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
public sealed class EmailEventIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public EmailEventIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendUserInviteEmailCommand_CompletesWithoutErrors()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var user = await _fixture.DatabaseManager.CreateUserAsync().ConfigureAwait(false);

        var userIdentityMock = GetUserIdentityForTest(new ExternalUserId(user.ExternalUserId));

        await _fixture.DatabaseManager.CreateEmailEventAsync(userIdentityMock.Email, EmailEventType.UserInvite).ConfigureAwait(false);

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository.Setup(e => e.GetAsync(userIdentityMock.Email)).ReturnsAsync(userIdentityMock);

        scope.Container!.Register(() => userIdentityRepository.Object, Lifestyle.Scoped);

        var command = new SendUserInviteEmailCommand();

        // Act + Assert
        var mediator = scope.GetInstance<IMediator>();
        await mediator.Send(command);
    }

    private UserIdentity GetUserIdentityForTest(ExternalUserId externalUserId)
    {
        return new UserIdentity(
            externalUserId,
            new EmailAddress("GetUserIdentityForTest@test.dk"),
            UserStatus.Active,
            "firstName",
            "lastName",
            new PhoneNumber("23232323"),
            DateTimeOffset.Now,
            AuthenticationMethod.Undetermined);
    }
}
