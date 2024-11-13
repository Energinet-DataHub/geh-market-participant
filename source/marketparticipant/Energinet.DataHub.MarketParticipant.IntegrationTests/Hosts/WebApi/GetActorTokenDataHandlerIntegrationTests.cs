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
    public async Task GetActorTokenDataHandler_ActorExists_TokenDataReturned()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var (_, _, actor) = await CreateActorWith2GridAccessProviderGridAreasAnd1EnergySupplierGridArea();

        var command = new GetActorTokenDataCommand(actor.Id);

        // act
        var actual = await mediator.Send(command);

        // assert
        Assert.Equal(actor.Id, actual.ActorTokenData.ActorId);
    }

    [Fact]
    public async Task GetActorTokenDataHandler_ActorExists_ReturnsOnlyThatActorsData()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        (GridAreaEntity GridAccessProviderGa1, GridAreaEntity GridAccessProviderGa2, ActorEntity Actor)[] actors =
        [
            await CreateActorWith2GridAccessProviderGridAreasAnd1EnergySupplierGridArea(),
            await CreateActorWith2GridAccessProviderGridAreasAnd1EnergySupplierGridArea(),
        ];

        foreach (var (gridAccessProviderGa1, gridAccessProviderGa2, actor) in actors)
        {
            var command = new GetActorTokenDataCommand(actor.Id);

            // act
            var actual = await mediator.Send(command);

            // assert
            Assert.NotNull(actual);
            Assert.Equal(actor.Id, actual.ActorTokenData.ActorId);
            Assert.Equal(actor.ActorNumber, actual.ActorTokenData.ActorNumber);
            Assert.Single(actual.ActorTokenData.MarketRoles);
            Assert.Single(actual.ActorTokenData.MarketRoles, x => x.Function == EicFunction.GridAccessProvider);

            var actualGridAccessProvider = actual.ActorTokenData.MarketRoles.Single(x => x.Function == EicFunction.GridAccessProvider);
            Assert.Equal(2, actualGridAccessProvider.GridAreas.Count());
            Assert.Single(actualGridAccessProvider.GridAreas, x => x.GridAreaCode == gridAccessProviderGa1.Code);
            Assert.Single(actualGridAccessProvider.GridAreas, x => x.GridAreaCode == gridAccessProviderGa2.Code);
        }
    }

    private async Task<(GridAreaEntity GridAccessProviderGa1, GridAreaEntity GridAccessProviderGa2, ActorEntity Actor)> CreateActorWith2GridAccessProviderGridAreasAnd1EnergySupplierGridArea()
    {
        var gridAccessProviderGa1 = await _fixture.PrepareGridAreaAsync();
        var gridAccessProviderGa2 = await _fixture.PrepareGridAreaAsync();

        var gridAccessProviderRole = new MarketRoleEntity
        {
            Function = EicFunction.GridAccessProvider,
            GridAreas =
            {
                new MarketRoleGridAreaEntity
                {
                    GridAreaId = gridAccessProviderGa1.Id,
                },
                new MarketRoleGridAreaEntity
                {
                    GridAreaId = gridAccessProviderGa2.Id,
                },
            },
        };

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            gridAccessProviderRole);

        return (gridAccessProviderGa1, gridAccessProviderGa2, actor);
    }
}
