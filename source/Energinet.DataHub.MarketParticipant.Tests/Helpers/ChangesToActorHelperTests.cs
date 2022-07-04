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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Helpers;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Helpers;

[UnitTest]
public class ChangesToActorHelperTests
{
    private readonly Actor _actor = CreateValidActorWithChildren();
    private readonly UpdateActorCommand _incomingActor = CreateValidIncomingActorWithChildren();

    [Fact]
    public void FindChangesMadeToActor_ExistingActorNull_ThrowsException()
    {
        // Arrange
        var target = new ChangesToActorHelper();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => target.FindChangesMadeToActor(null!, _incomingActor));
    }

    [Fact]
    public void FindChangesMadeToActor_IncomingNull_ThrowsException()
    {
        // Arrange
        var target = new ChangesToActorHelper();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => target.FindChangesMadeToActor(_actor, null!));
    }

    [Fact]
    public void FindChangesMadeToActor_NewDataIncoming_ChangesAreFoundAndIntegrationEventsAreaReturned()
    {
        // Arrange
        var target = new ChangesToActorHelper();

        // Act
        var result = target.FindChangesMadeToActor(_actor, _incomingActor).ToList();

        // Assert
        var numberOfStatusChangedEvents = result.Count(x => x is ActorStatusChangedIntegrationEvent);
        var numberOfAddMeteringPointEvents = result.Count(x => x is AddMeteringPointTypeIntegrationEvent);
        var numberOfRemoveMeteringPointEvents = result.Count(x => x is RemoveMeteringPointTypeIntegrationEvent);
        var numberOfAddGridAreaEvents = result.Count(x => x is AddGridAreaIntegrationEvent);
        var numberOfRemoveGridAreaEvents = result.Count(x => x is RemoveGridAreaIntegrationEvent);
        var numberOfAddMarketRoleEvents = result.Count(x => x is AddMarketRoleIntegrationEvent);
        var numberOfRemoveMarketRoleEvents = result.Count(x => x is RemoveMarketRoleIntegrationEvent);

        Assert.Equal(1, numberOfStatusChangedEvents);
        Assert.Equal(2, numberOfAddMeteringPointEvents);
        Assert.Equal(6, numberOfRemoveMeteringPointEvents);
        Assert.Equal(1, numberOfAddGridAreaEvents);
        Assert.Equal(2, numberOfRemoveGridAreaEvents);
        Assert.Equal(1, numberOfAddMarketRoleEvents);
        Assert.Equal(1, numberOfRemoveMarketRoleEvents);
        Assert.Equal(14, result.Count);
    }

    private static Actor CreateValidActorWithChildren()
    {
        return new Actor(
            Guid.Parse("83d845e5-567d-41bb-bfc5-e062e56fb23c"),
            new ExternalActorId(Guid.NewGuid()),
            new ActorNumber("1234567890123"),
            ActorStatus.Active,
            new List<ActorMarketRole>
            {
                new ActorMarketRole(
                    Guid.Parse("579010ed-b960-486f-857f-a7c020ffed4d"),
                    EicFunction.EnergySupplier,
                    new List<ActorGridArea>
                    {
                        new ActorGridArea(
                            Guid.Parse("02222dec-9ac7-4732-80e3-3e943501e93d"),
                            new List<MeteringPointType>
                            {
                                MeteringPointType.E17Consumption
                            })
                    }),
                new ActorMarketRole(
                    Guid.Parse("8bd18c6e-c971-4be8-93cf-e3d4345a2d14"),
                    EicFunction.Producer,
                    new List<ActorGridArea>
                    {
                        new ActorGridArea(
                            Guid.Parse("2aca6c52-3282-40e5-a071-c740c9d432b6"),
                            new List<MeteringPointType>
                            {
                                MeteringPointType.D02Analysis,
                                MeteringPointType.E17Consumption,
                                MeteringPointType.E18Production
                            }),
                        new ActorGridArea(
                            Guid.Parse("35d007b1-12d0-470f-8186-231b9e51f9e0"),
                            new List<MeteringPointType>
                            {
                                MeteringPointType.E20Exchange,
                                MeteringPointType.D01VeProduction
                            })
                    })
            });
    }

    private static UpdateActorCommand CreateValidIncomingActorWithChildren()
    {
        return new UpdateActorCommand(
            Guid.NewGuid(),
            Guid.Parse("83d845e5-567d-41bb-bfc5-e062e56fb23c"),
            new ChangeActorDto(
                "Passive",
                new List<ActorMarketRoleDto>
                {
                    new ActorMarketRoleDto(
                        EicFunction.EnergySupplier.ToString(),
                        new List<ActorGridAreaDto>
                        {
                            new ActorGridAreaDto(
                                Guid.Parse("02222dec-9ac7-4732-80e3-3e943501e93d"),
                                new List<string>
                                {
                                    "Unknown"
                                })
                        }),
                    new ActorMarketRoleDto(
                        EicFunction.BillingAgent.ToString(),
                        new List<ActorGridAreaDto>
                        {
                            new ActorGridAreaDto(
                                Guid.NewGuid(),
                                new List<string>
                                {
                                    "D05NetProduction"
                                })
                        })
                }));
    }
}
