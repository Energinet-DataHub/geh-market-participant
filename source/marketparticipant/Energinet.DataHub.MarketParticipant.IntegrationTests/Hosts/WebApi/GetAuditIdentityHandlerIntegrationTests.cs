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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetAuditIdentityHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetAuditIdentityHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAuditIdentity_KnownIdentity_ReturnsDisplayName()
    {
        // Arrange
        var user = await _fixture.PrepareUserAsync();

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(user.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var command = new GetAuditIdentityCommand(KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId.Value);
        var actual = await mediator.Send(command);

        // Assert
        Assert.Equal("DataHub", actual.DisplayName);
    }

    [Fact]
    public async Task GetAuditIdentity_KnownIdentityAsFas_ReturnsDisplayName()
    {
        // Arrange
        var user = await _fixture.PrepareUserAsync();

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(user.Id, true);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var command = new GetAuditIdentityCommand(KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId.Value);
        var actual = await mediator.Send(command);

        // Assert
        Assert.Equal($"DataHub ({KnownAuditIdentityProvider.OrganizationBackgroundService.FriendlyName})", actual.DisplayName);
    }

    [Fact]
    public async Task GetAuditIdentity_UserFromDifferentActor_ReturnsOrganizationName()
    {
        // Arrange
        var auditUser = await _fixture.PrepareUserAsync();
        var loginUser = await _fixture.PrepareUserAsync();

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(loginUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var command = new GetAuditIdentityCommand(auditUser.Id);
        var actual = await mediator.Send(command);

        // Assert
        Assert.Equal(TestPreparationEntities.ValidOrganization.Name, actual.DisplayName);
    }

    [Fact]
    public async Task GetAuditIdentity_UserIsSameOrganization_ReturnsFullDisplayName()
    {
        // Arrange
        var auditUser = await _fixture.PrepareUserAsync();
        var loginUser = await _fixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(u => u.AdministratedByActorId = auditUser.AdministratedByActorId));

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(loginUser);

        var userIdentityMock = new Mock<IUserIdentityRepository>();
        var externalUserId = new ExternalUserId(auditUser.ExternalId);
        var userIdentity = new UserIdentity(
            externalUserId,
            new MockedEmailAddress(),
            UserIdentityStatus.Active,
            "expected_first_name",
            "expected_last_name",
            null,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());

        userIdentityMock
            .Setup(repository => repository.GetAsync(externalUserId))
            .ReturnsAsync(userIdentity);

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityMock.Object);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var command = new GetAuditIdentityCommand(auditUser.Id);
        var actual = await mediator.Send(command);

        // Assert
        Assert.Equal($"{userIdentity.FirstName} ({userIdentity.Email})", actual.DisplayName);
    }
}
