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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class ActorStatusMarketRolesRuleServiceTests
{
    [Fact]
    public async Task Validate_ActorIsNotFound_Throws()
    {
        var actorRepositoryMock = new Mock<IActorRepository>();

        var target = new ActorStatusMarketRolesRuleService(actorRepositoryMock.Object);

        var updatedActor = CreateActor(ActorStatus.Active, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);

        // act + assert
        var exc = await Assert.ThrowsAsync<NotFoundValidationException>(() =>
            target.ValidateAsync(updatedActor));

        Assert.Equal("Actor not found", exc.Message);
    }

    [Theory]
    [InlineData(ActorStatus.New)]
    [InlineData(ActorStatus.Active)]
    [InlineData(ActorStatus.Inactive)]
    [InlineData(ActorStatus.Passive)]
    public async Task Validate_UpdatedActorHasIdenticalMarketRoles_DoesNotThrow(ActorStatus status)
    {
        // arrange
        var updatedActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(updatedActor.Id))
            .ReturnsAsync(updatedActor);

        var target = new ActorStatusMarketRolesRuleService(actorRepositoryMock.Object);

        // act + assert
        await target.ValidateAsync(updatedActor);
    }

    [Theory]
    [InlineData(ActorStatus.New)]
    [InlineData(ActorStatus.Active)]
    [InlineData(ActorStatus.Inactive)]
    [InlineData(ActorStatus.Passive)]
    public async Task Validate_UpdatedActorHasNewMarketRoleAdded_DoesNotThrow(ActorStatus status)
    {
        // arrange
        var existingActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);

        var updatedActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);
        updatedActor.MarketRoles.Add(new ActorMarketRole(EicFunction.BalanceResponsibleParty, new[] { new ActorGridArea(new[] { MeteringPointType.D03NotUsed }) }));

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(updatedActor.Id))
            .ReturnsAsync(existingActor);

        var target = new ActorStatusMarketRolesRuleService(actorRepositoryMock.Object);

        // act + assert
        await target.ValidateAsync(updatedActor);
    }

    [Theory]
    [InlineData(ActorStatus.New, false)]
    [InlineData(ActorStatus.Active, true)]
    [InlineData(ActorStatus.Inactive, true)]
    [InlineData(ActorStatus.Passive, true)]
    public async Task Validate_GridAreaForMarketRoleIsRemoved_ThrowsIfStatusIsNotNew(ActorStatus status, bool throws)
    {
        // arrange
        var existingActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);

        var updatedActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);
        updatedActor.MarketRoles.Single().GridAreas.Clear();

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(updatedActor.Id))
            .ReturnsAsync(existingActor);

        var target = new ActorStatusMarketRolesRuleService(actorRepositoryMock.Object);

        // act + assert
        if (throws)
            await Assert.ThrowsAsync<ValidationException>(() => target.ValidateAsync(updatedActor));
    }

    [Theory]
    [InlineData(ActorStatus.New, false)]
    [InlineData(ActorStatus.Active, true)]
    [InlineData(ActorStatus.Inactive, true)]
    [InlineData(ActorStatus.Passive, true)]
    public async Task Validate_UpdatedActorHasUpdatedMeteringPointForMarketRoleGridArea_Throws(ActorStatus status, bool throws)
    {
        // arrange
        var existingActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);

        var updatedActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);
        updatedActor.MarketRoles.Single().GridAreas.Single().MeteringPointTypes.Add(MeteringPointType.D02Analysis);

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(updatedActor.Id))
            .ReturnsAsync(existingActor);

        var target = new ActorStatusMarketRolesRuleService(actorRepositoryMock.Object);

        // act + assert
        if (throws)
            await Assert.ThrowsAsync<ValidationException>(() => target.ValidateAsync(updatedActor));
    }

    [Theory]
    [InlineData(ActorStatus.New, false)]
    [InlineData(ActorStatus.Active, true)]
    [InlineData(ActorStatus.Inactive, true)]
    [InlineData(ActorStatus.Passive, true)]
    public async Task Validate_UpdatedActorWithNoMarketRoles_Throws(ActorStatus status, bool throws)
    {
        // arrange
        var existingActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);

        var updatedActor = CreateActor(status, EicFunction.BillingAgent, MeteringPointType.D01VeProduction);
        updatedActor.MarketRoles.Clear();

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(updatedActor.Id))
            .ReturnsAsync(existingActor);

        var target = new ActorStatusMarketRolesRuleService(actorRepositoryMock.Object);

        // act + assert
        if (throws)
            await Assert.ThrowsAsync<ValidationException>(() => target.ValidateAsync(updatedActor));
    }

    private static Actor CreateActor(ActorStatus status, EicFunction eicFunction, MeteringPointType meteringPointType)
    {
        return new Actor(
            new ActorId(Guid.Parse("9B6CF046-94AC-4210-8D8E-138032F17AAB")),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            status,
            new[]
            {
                new ActorMarketRole(
                    eicFunction,
                    new[]
                    {
                        new ActorGridArea(new[]
                        {
                            meteringPointType
                        })
                    })
            },
            new ActorName("actor name"));
    }
}
