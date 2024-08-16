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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetActorTokenDataHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetActorTokenDataHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetActorTokenDataHandler_ActorIdCorrect_ReturnsData()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var gridArea = await _fixture.PrepareGridAreaAsync();

        var marketRole = new MarketRoleEntity
        {
            Function = EicFunction.GridAccessProvider,
            GridAreas =
            {
                new MarketRoleGridAreaEntity
                {
                    GridAreaId = gridArea.Id,
                },
            },
        };

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            marketRole);

        var command = new GetActorTokenDataCommand(
            actor.Id);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.NotNull(actual);
        Assert.Equal(actor.Id, actual.ActorTokenData.ActorId);
        Assert.Equal(actor.ActorNumber, actual.ActorTokenData.ActorNumber);
        Assert.Equal(marketRole.Function, actual.ActorTokenData.MarketRoles.Single().Function);
        Assert.Equal(gridArea.Id, actual.ActorTokenData.MarketRoles.Single().GridAreas.Single().GridAreaId);
        Assert.Equal(gridArea.Code, actual.ActorTokenData.MarketRoles.Single().GridAreas.Single().GridAreaCode);
    }
}
