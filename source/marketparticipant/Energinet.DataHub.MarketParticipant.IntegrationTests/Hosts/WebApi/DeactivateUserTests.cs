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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code")]
public sealed class DeactivateUserHandlerTests : WebApiIntegrationTestsBase
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public DeactivateUserHandlerTests(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Deactivate_UserExists_UserIsDeactivated()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        scope.Container!.Register(() => userIdentityRepositoryMock.Object);

        var userContext = new Mock<IUserContext<FrontendUser>>();
        scope.Container!.Register(() => userContext.Object);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new MockedEmailAddress(),
            UserIdentityStatus.Active,
            "first",
            "last",
            null,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        var userEntity = await _fixture.PrepareUserAsync();

        userContext.Setup(x => x.CurrentUser).Returns(new FrontendUser(userEntity.Id, Guid.NewGuid(), Guid.NewGuid(), true));

        userIdentityRepositoryMock
            .Setup(x => x.GetAsync(new ExternalUserId(userEntity.ExternalId)))
            .ReturnsAsync(userIdentity);

        var mediator = scope.GetInstance<IMediator>();
        var command = new DeactivateUserCommand(userEntity.Id);

        // act
        await mediator.Send(command);

        // assert
        userIdentityRepositoryMock.Verify(x => x.DisableUserAccountAsync(userIdentity.Id), Times.Once);
    }
}
