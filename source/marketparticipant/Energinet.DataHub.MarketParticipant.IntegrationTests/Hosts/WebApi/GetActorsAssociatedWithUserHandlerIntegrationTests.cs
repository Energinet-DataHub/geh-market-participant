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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetActorsAssociatedWithUserHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetActorsAssociatedWithUserHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetActorsAssociatedWithUser_GivenUserId_ReturnsActors()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.UsersManage);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var command = new GetActorsAssociatedWithUserCommand(user.Id);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.Single(actual.ActorIds);
        Assert.Equal(actor.Id, actual.ActorIds.Single());
        Assert.Equal(user.AdministratedByActorId, actual.AdministratedBy);
    }

    [Fact]
    public async Task GetActorsAssociatedWithUser_NoUserId_ThrowsNotFoundValidationException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var command = new GetActorsAssociatedWithUserCommand(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => mediator.Send(command));
    }
}
