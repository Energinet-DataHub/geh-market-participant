﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Domain.Model;
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

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.Organization;

[Collection(nameof(IntegrationTestCollectionFixture))]
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

        var user = await _fixture.PrepareUserAsync();

        var userIdentityMock = GetUserIdentityForTest(new ExternalUserId(user.ExternalId));

        await _fixture.EmailEventsClearNotSentAsync();
        await _fixture.PrepareEmailEventAsync(TestPreparationEntities.ValidEmailEvent.Patch(t => t.Email = userIdentityMock.Email.Address));

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(e => e.GetAsync(userIdentityMock.Email))
            .ReturnsAsync(userIdentityMock);

        host.ServiceCollection.RemoveAll<IUserIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => userIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var command = new SendEmailCommand();

        // Act + Assert
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(command);
    }

    private static UserIdentity GetUserIdentityForTest(ExternalUserId externalUserId)
    {
        return new UserIdentity(
            externalUserId,
            new RandomlyGeneratedEmailAddress(),
            UserIdentityStatus.Active,
            "firstName",
            "lastName",
            new PhoneNumber("23232323"),
            DateTimeOffset.Now,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());
    }
}
