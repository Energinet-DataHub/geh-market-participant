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
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetBalanceResponsibilityRelationsHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task GetBalanceResponsibility_NoRelations_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actor = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var createCommand = new GetBalanceResponsibilityRelationsCommand(actor.Id);

        // Act
        var response = await mediator.Send(createCommand);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Relations);
    }

    [Fact]
    public async Task GetBalanceResponsibility_ForEnergySupplier_ReturnsRelations()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var gridArea = await fixture.PrepareGridAreaAsync();

        var actorA = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var actorB = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.BalanceResponsibleParty });

        var balanceResponsibilityRequest = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E18Production,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToInstant(),
            null);

        var balanceResponsibilityRequestRepository = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        await balanceResponsibilityRequestRepository.EnqueueAsync(balanceResponsibilityRequest);

        var createCommand = new GetBalanceResponsibilityRelationsCommand(actorA.Id);

        // Act
        var response = await mediator.Send(createCommand);

        // Assert
        Assert.Single(response.Relations);
        var actual = response.Relations.Single();
        Assert.Equal(actorA.Id, actual.EnergySupplierId);
        Assert.Equal(actorB.Id, actual.BalanceResponsibleId);
        Assert.Equal(gridArea.Id, actual.GridAreaId);
        Assert.Equal(balanceResponsibilityRequest.MeteringPointType, actual.MeteringPointType);
        Assert.Equal(balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset(), actual.ValidFrom);
        Assert.Null(actual.ValidTo);
    }

    [Fact]
    public async Task GetBalanceResponsibility_ForBalanceResponsibleParty_ReturnsRelations()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var gridArea = await fixture.PrepareGridAreaAsync();

        var actorA = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var actorB = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.BalanceResponsibleParty });

        var balanceResponsibilityRequest = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorB.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E18Production,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToInstant(),
            null);

        var balanceResponsibilityRequestRepository = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        await balanceResponsibilityRequestRepository.EnqueueAsync(balanceResponsibilityRequest);

        var createCommand = new GetBalanceResponsibilityRelationsCommand(actorB.Id);

        // Act
        var response = await mediator.Send(createCommand);

        // Assert
        Assert.Single(response.Relations);
        Assert.Single(response.Relations, relation =>
            relation.EnergySupplierId == actorA.Id &&
            relation.BalanceResponsibleId == actorB.Id &&
            relation.GridAreaId == gridArea.Id &&
            relation.MeteringPointType == balanceResponsibilityRequest.MeteringPointType &&
            relation.ValidFrom == balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset() &&
            relation.ValidTo == null);
    }

    [Fact]
    public async Task GetBalanceResponsibility_ForEnergySupplierWithBrpOtherRelations_ReturnsOnlyForEnergySupplier()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var gridArea = await fixture.PrepareGridAreaAsync();

        var actorA = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var actorB = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var actorC = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.BalanceResponsibleParty });

        var balanceResponsibilityRequestA = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorC.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E18Production,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToInstant(),
            null);

        var balanceResponsibilityRequestB = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorB.ActorNumber),
            ActorNumber.Create(actorC.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E18Production,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToInstant(),
            null);

        var balanceResponsibilityRequestRepository = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        await balanceResponsibilityRequestRepository.EnqueueAsync(balanceResponsibilityRequestA);
        await balanceResponsibilityRequestRepository.EnqueueAsync(balanceResponsibilityRequestB);

        var createCommand = new GetBalanceResponsibilityRelationsCommand(actorA.Id);

        // Act
        var response = await mediator.Send(createCommand);

        // Assert
        Assert.Single(response.Relations);
        Assert.Single(response.Relations, relation =>
            relation.EnergySupplierId == actorA.Id &&
            relation.BalanceResponsibleId == actorC.Id &&
            relation.GridAreaId == gridArea.Id &&
            relation.MeteringPointType == balanceResponsibilityRequestA.MeteringPointType &&
            relation.ValidFrom == balanceResponsibilityRequestA.ValidFrom.ToDateTimeOffset() &&
            relation.ValidTo == null);
    }

    [Fact]
    public async Task GetBalanceResponsibility_ForBrpWithMultipleEnergySuppliers_ReturnsBothEnergySuppliers()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var gridArea = await fixture.PrepareGridAreaAsync();

        var actorA = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var actorB = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.EnergySupplier });

        var actorC = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            new MarketRoleEntity { Function = EicFunction.BalanceResponsibleParty });

        var balanceResponsibilityRequestA = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorA.ActorNumber),
            ActorNumber.Create(actorC.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E18Production,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToInstant(),
            null);

        var balanceResponsibilityRequestB = new BalanceResponsibilityRequest(
            ActorNumber.Create(actorB.ActorNumber),
            ActorNumber.Create(actorC.ActorNumber),
            new GridAreaCode(gridArea.Code),
            MeteringPointType.E18Production,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToInstant(),
            null);

        var balanceResponsibilityRequestRepository = scope.ServiceProvider.GetRequiredService<IBalanceResponsibilityRequestRepository>();
        await balanceResponsibilityRequestRepository.EnqueueAsync(balanceResponsibilityRequestA);
        await balanceResponsibilityRequestRepository.EnqueueAsync(balanceResponsibilityRequestB);

        var createCommand = new GetBalanceResponsibilityRelationsCommand(actorC.Id);

        // Act
        var response = await mediator.Send(createCommand);

        // Assert
        Assert.Equal(2, response.Relations.Count());
    }
}
