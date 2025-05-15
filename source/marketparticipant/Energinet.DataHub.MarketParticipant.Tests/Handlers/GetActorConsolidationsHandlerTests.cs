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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetActorConsolidationsHandlerTests
{
    [Fact]
    public async Task Handle_NoConsolidations_ReturnsEmpty()
    {
        // Arrange
        var actorConsolidationsRepositoryMock = new Mock<IActorConsolidationRepository>();
        var target = new GetActorConsolidationsHandler(actorConsolidationsRepositoryMock.Object);
        var command = new GetActorConsolidationsCommand();

        // Act + Assert
        var result = await target.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.ActorConsolidations);
    }

    [Fact]
    public async Task Handle_HasConsolidations_ReturnsConsolidations()
    {
        // Arrange
        var actorConsolidationsRepositoryMock = new Mock<IActorConsolidationRepository>();
        var target = new GetActorConsolidationsHandler(actorConsolidationsRepositoryMock.Object);
        var consolidation = TestPreparationModels.MockedActorConsolidation();
        var consolidation2 = TestPreparationModels.MockedActorConsolidation();
        actorConsolidationsRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync())
            .ReturnsAsync([consolidation, consolidation2]);

        var command = new GetActorConsolidationsCommand();

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.ActorConsolidations.Count());
        Assert.Equal(consolidation.ActorToId.Value, response.ActorConsolidations.First().ActorToId);
        Assert.Equal(consolidation2.ActorToId.Value, response.ActorConsolidations.Skip(1).First().ActorToId);
    }
}
