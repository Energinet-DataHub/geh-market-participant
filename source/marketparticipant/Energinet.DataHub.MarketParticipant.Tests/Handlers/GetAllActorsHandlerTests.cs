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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetAllActorsHandlerTests
{
    [Fact]
    public async Task Handle_NoActors_ReturnsEmptyList()
    {
        // Arrange
        var target = new GetAllActorsHandler(new Mock<IActorRepository>().Object);

        var command = new GetAllActorsCommand();

        // Act
        var actual = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(actual.Actors);
        Assert.Empty(actual.Actors);
    }

    [Fact]
    public async Task Handle_AllActors_ReturnsActors()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var target = new GetAllActorsHandler(actorRepositoryMock.Object);

        var orgId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var actorId2 = Guid.NewGuid();

        var actor = TestPreparationModels.MockedActor(actorId, orgId);
        var actor2 = TestPreparationModels.MockedActor(actorId2, orgId);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetActorsAsync())
            .ReturnsAsync(new[] { actor, actor2 });

        var command = new GetAllActorsCommand();

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEmpty(response.Actors);
        Assert.Equal(2, response.Actors.Count());

        var firstActor = response.Actors.First();
        var secondActor = response.Actors.Skip(1).First();

        Assert.Equal(actor.Id.Value, firstActor.ActorId);
        Assert.Equal(actor2.Id.Value, secondActor.ActorId);
    }
}
