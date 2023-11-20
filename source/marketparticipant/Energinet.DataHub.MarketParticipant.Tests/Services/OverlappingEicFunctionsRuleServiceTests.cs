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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

public sealed class OverlappingEicFunctionsRuleServiceTests
{
    [Fact]
    public async Task Validate_OrganizationHasActorWithDifferentGln_DoesNotThrow()
    {
        // Arrange
        var repository = new Mock<IActorRepository>();
        var target = new OverlappingEicFunctionsRuleService(repository.Object);

        var actorExisting = CreateActorForTest(EicFunction.GridAccessProvider);

        var actor = new Actor(actorExisting.OrganizationId, new MockedGln(), actorExisting.Name);
        actor.AddMarketRole(new ActorMarketRole(EicFunction.GridAccessProvider));

        repository
            .Setup(repo => repo.GetActorsAsync(actor.OrganizationId))
            .ReturnsAsync(new[]
            {
                actorExisting
            });

        // Act + Assert
        await target.ValidateEicFunctionsAcrossActorsAsync(actor);
    }

    [Fact]
    public async Task Validate_OrganizationHasActorWithSameGlnDifferentRole_DoesNotThrow()
    {
        // Arrange
        var repository = new Mock<IActorRepository>();
        var target = new OverlappingEicFunctionsRuleService(repository.Object);

        var actorExisting = CreateActorForTest(EicFunction.GridAccessProvider);

        var actor = new Actor(actorExisting.OrganizationId, actorExisting.ActorNumber, actorExisting.Name);
        actor.AddMarketRole(new ActorMarketRole(EicFunction.EnergySupplier));

        repository
            .Setup(repo => repo.GetActorsAsync(actor.OrganizationId))
            .ReturnsAsync(new[]
            {
                actorExisting
            });

        // Act + Assert
        await target.ValidateEicFunctionsAcrossActorsAsync(actor);
    }

    [Fact]
    public async Task Validate_OrganizationHasActorWithSameGlnSameRole_ThrowsValidationException()
    {
        // Arrange
        var repository = new Mock<IActorRepository>();
        var target = new OverlappingEicFunctionsRuleService(repository.Object);

        var actorExisting = CreateActorForTest(EicFunction.EnergySupplier);

        var actor = new Actor(actorExisting.OrganizationId, actorExisting.ActorNumber, actorExisting.Name);
        actor.AddMarketRole(new ActorMarketRole(EicFunction.EnergySupplier));

        repository
            .Setup(repo => repo.GetActorsAsync(actor.OrganizationId))
            .ReturnsAsync(new[]
            {
                actorExisting
            });

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.ValidateEicFunctionsAcrossActorsAsync(actor));
    }

    [Fact]
    public async Task Validate_OrganizationHasNoActors_ThrowsValidationException()
    {
        // Arrange
        var repository = new Mock<IActorRepository>();

        var target = new OverlappingEicFunctionsRuleService(repository.Object);

        var actor = CreateActorForTest(EicFunction.DataHubAdministrator);

        repository
            .Setup(repo => repo.GetActorsAsync(actor.OrganizationId))
            .ReturnsAsync(Array.Empty<Actor>());

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.ValidateEicFunctionsAcrossActorsAsync(actor));
    }

    [Fact]
    public async Task Validate_OrganizationHasActorsIncorrectMarketRole_ThrowsValidationException()
    {
        // Arrange
        var repository = new Mock<IActorRepository>();

        var target = new OverlappingEicFunctionsRuleService(repository.Object);

        var actor = CreateActorForTest(EicFunction.DataHubAdministrator);

        repository
            .Setup(repo => repo.GetActorsAsync(actor.OrganizationId))
            .ReturnsAsync(new[]
            {
                CreateActorForTest(EicFunction.BalanceResponsibleParty),
                CreateActorForTest(EicFunction.BillingAgent),
            });

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.ValidateEicFunctionsAcrossActorsAsync(actor));
    }

    [Fact]
    public async Task Validate_OrganizationHasDataHubAdministratorActors_DoesNotThrow()
    {
        // Arrange
        var repository = new Mock<IActorRepository>();

        var target = new OverlappingEicFunctionsRuleService(repository.Object);

        var actor = CreateActorForTest(EicFunction.DataHubAdministrator);

        repository
            .Setup(repo => repo.GetActorsAsync(actor.OrganizationId))
            .ReturnsAsync(new[]
            {
                CreateActorForTest(EicFunction.DataHubAdministrator),
                CreateActorForTest(EicFunction.BillingAgent),
            });

        // Act + Assert
        await target.ValidateEicFunctionsAcrossActorsAsync(actor);
    }

    private static Actor CreateActorForTest(EicFunction marketRole)
    {
        return new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new[] { new ActorMarketRole(marketRole) },
            new ActorName("fake_value"),
            null);
    }
}
