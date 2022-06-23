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
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Helpers;

[UnitTest]
public class ChangesToActorHelperTests
{
    private readonly UpdateActorCommand _incomingActor = new UpdateActorCommand(
        Guid.NewGuid(),
        Guid.NewGuid(),
        new ChangeActorDto(
            "Active",
            new List<Guid> { Guid.NewGuid() },
            new List<ActorMarketRoleDto>
            {
                new ActorMarketRoleDto(
                    EicFunction.Agent.ToString(),
                    new List<ActorGridAreaDto> { new ActorGridAreaDto(Guid.NewGuid(), new List<string> { "Unknown" }) })
            },
            new List<string> { "Unknown" }));

    private readonly Actor _actor = new Actor(
        Guid.NewGuid(),
        new ExternalActorId(Guid.NewGuid()),
        new ActorNumber("1234567890123"),
        ActorStatus.Active,
        Enumerable.Empty<ActorMarketRole>());

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


}
