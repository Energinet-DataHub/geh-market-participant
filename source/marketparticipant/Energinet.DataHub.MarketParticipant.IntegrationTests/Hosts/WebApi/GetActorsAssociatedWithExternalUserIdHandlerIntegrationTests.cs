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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetActorsAssociatedWithExternalUserIdHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public GetActorsAssociatedWithExternalUserIdHandlerIntegrationTests(
        MarketParticipantDatabaseFixture fixture,
        GraphServiceClientFixture graphServiceClientFixture)
    {
        _fixture = fixture;
        _graphServiceClientFixture = graphServiceClientFixture;
    }

    [Fact]
    public async Task GetActorsAssociatedWithExternalUserId_GivenUserId_ReturnsActors()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActiveActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var command = new GetActorsAssociatedWithExternalUserIdCommand(user.ExternalId);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.Single(actual.ActorIds);
        Assert.Equal(actor.Id, actual.ActorIds.Single());
    }

    [Fact]
    public async Task GetActorsAssociatedWithExternalUserId_NoExternalUserId_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetActorsAssociatedWithExternalUserIdCommand(Guid.NewGuid());

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.Empty(actual.ActorIds);
    }

    [Fact]
    public async Task GetActorsAssociatedWithExternalUserId_GivenUserIdWithoutAssignments_ReturnsEmptyList()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();

        var command = new GetActorsAssociatedWithExternalUserIdCommand(user.ExternalId);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.Empty(actual.ActorIds);
    }

    [Fact]
    public async Task GetActorsAssociatedWithExternalUserId_GivenOpenId_ReturnsActors()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var openIdUserEmail = new RandomlyGeneratedEmailAddress();
        var openIdIdentity = new List<ObjectIdentity>
        {
            new()
            {
                SignInType = "federated",
                Issuer = Guid.NewGuid().ToString(),
                IssuerAssignedId = Guid.NewGuid().ToString(),
            },
        };

        var externalUserId = await _graphServiceClientFixture.CreateUserAsync(openIdUserEmail);
        var openIdExternalUserId = await _graphServiceClientFixture.CreateUserAsync(openIdUserEmail, openIdIdentity);

        var actor = await _fixture.PrepareActiveActorAsync();
        var user = await _fixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser.Patch(u => u.ExternalId = externalUserId.Value));
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var command = new GetActorsAssociatedWithExternalUserIdCommand(openIdExternalUserId.Value);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        Assert.Single(actual.ActorIds);
        Assert.Equal(actor.Id, actual.ActorIds.Single());
    }
}
