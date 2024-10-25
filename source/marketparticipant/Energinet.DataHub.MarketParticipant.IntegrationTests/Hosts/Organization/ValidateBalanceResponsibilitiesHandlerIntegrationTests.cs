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
using AutoFixture;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Application.Contracts;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Integration;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.Organization;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ValidateBalanceResponsibilitiesHandlerIntegrationTests : IAsyncLifetime
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ValidateBalanceResponsibilitiesHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        return _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.DataHubAdministrator));
    }

    public async Task DisposeAsync()
    {
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var cleanup = await context.Actors
            .Include(a => a.MarketRoles)
            .Where(a => a.MarketRoles.Any(mr => mr.Function == EicFunction.DataHubAdministrator))
            .ToListAsync();

        foreach (var entity in cleanup)
        {
            context.Actors.Remove(entity);
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_EnergySupplierUnrecognized_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplierId = new MockedGln();
        var balanceResponsibleParty = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplierId,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
            It.Is<BalanceResponsibilityValidationFailed>(notification =>
                notification.IsActorUnrecognized == true &&
                notification.ActorNumber.Value == energySupplierId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BalanceResponsibilityPartyUnrecognized_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var balanceResponsiblePartyId = new MockedGln();
        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyId,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
            It.Is<BalanceResponsibilityValidationFailed>(notification =>
                notification.IsActorUnrecognized == true &&
                notification.ActorNumber.Value == balanceResponsiblePartyId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InactiveActorWithOverlap_IgnoreInactive()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor.Patch(a => a.Status = ActorStatus.Inactive),
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsibleParty = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2023, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidOverlap_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsibleParty = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2023, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StoppedRecently_IgnoresStop()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsibleParty = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp()
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StoppedRecentlyButToday_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsibleParty = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = null
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = DateTimeOffset.UtcNow.AddHours(1).ToTimestamp()
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TwoBrpOverlapping_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsiblePartyA = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));
        var balanceResponsiblePartyB = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        var now = DateTime.UtcNow;

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyA.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = now.AddDays(1).ToTimestamp()
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyA.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = now.AddDays(1).ToTimestamp(),
            ValidTo = null
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyB.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = now.AddDays(1).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OverlapWithGap_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsibleParty = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = DateTime.UtcNow.AddDays(1).ToTimestamp()
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsibleParty.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = DateTime.UtcNow.AddDays(5).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidOverlap_DoesNothing()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var ga = await _fixture.PrepareGridAreaAsync();

        var energySupplier = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.EnergySupplier));
        var balanceResponsiblePartyA = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));
        var balanceResponsiblePartyB = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(mr => mr.Function = EicFunction.BalanceResponsibleParty));

        await using var scope = host.BeginScope();
        var balanceResponsiblePartiesChangedEventHandler = scope.ServiceProvider.GetRequiredService<IBalanceResponsiblePartiesChangedEventHandler>();

        var now = DateTime.UtcNow;

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyA.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = new DateTime(2024, 10, 8, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = now.AddDays(1).ToTimestamp()
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyB.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = now.AddDays(1).ToTimestamp(),
            ValidTo = now.AddDays(5).ToTimestamp()
        });

        await balanceResponsiblePartiesChangedEventHandler.HandleAsync(new BalanceResponsiblePartiesChanged
        {
            EnergySupplierId = energySupplier.ActorNumber,
            BalanceResponsibleId = balanceResponsiblePartyA.ActorNumber,
            GridAreaCode = ga.Code,
            MeteringPointType = Application.Contracts.MeteringPointType.Production,
            Received = DateTime.UtcNow.ToTimestamp(),
            ValidFrom = now.AddDays(5).ToTimestamp(),
            ValidTo = null
        });

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateBalanceResponsibilitiesCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<BalanceResponsibilityValidationFailed>(notification =>
                    notification.IsActorUnrecognized == false &&
                    notification.ActorNumber.Value == energySupplier.ActorNumber)),
            Times.Never);
    }
}
