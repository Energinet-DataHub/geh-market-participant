﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class IntegrationEventProviderIntegrationTests : IAsyncLifetime
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public IntegrationEventProviderIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.DatabaseManager.DeleteDatabaseAsync();
        await _fixture.DatabaseManager.CreateDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAsync_NoEvents_DoesNothing()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IIntegrationEventProvider>();

        // Act + Assert
        var integrationEvents = target.GetAsync();

        await foreach (var unused in integrationEvents)
        {
            Assert.Fail("Should be empty.");
        }
    }

    [Fact]
    public async Task GetAsync_WithEvent_IntegrationEventReturned()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var allIntegrationEvents = typeof(DomainEvent)
            .Assembly
            .GetTypes()
            .Where(domainEventType => domainEventType.IsSubclassOf(typeof(DomainEvent)) && !domainEventType.IsAbstract)
            .ToDictionary(x => x.Name);

        foreach (var (eventName, eventType) in allIntegrationEvents)
        {
            await PrepareDomainEventAsync(scope.ServiceProvider, eventType);

            var target = scope.ServiceProvider.GetRequiredService<IIntegrationEventProvider>();

            // Act
            var integrationEvents = target
                .GetAsync()
                .ToBlockingEnumerable()
                .ToList();

            // Assert
            Assert.Single(integrationEvents);

            Assert.Equal(
                eventType.IsSubclassOf(typeof(NotificationEvent))
                    ? "UserNotificationTriggered"
                    : eventName,
                integrationEvents[0].EventName);
        }
    }

    private static async Task PrepareBalanceResponsibilityValidationFailedEventAsync(IServiceProvider scope)
    {
        var notification = new BalanceResponsibilityValidationFailed(new ActorId(Guid.NewGuid()), new MockedGln(), true);

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(notification);
    }

    private static async Task PrepareNewBalanceResponsibilityReceivedEventAsync(IServiceProvider scope)
    {
        var notification = new NewBalanceResponsibilityReceived(new ActorId(Guid.NewGuid()), new MockedGln());

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(notification);
    }

    private static async Task PrepareActorCredentialsExpiringEventAsync(IServiceProvider scope)
    {
        var recipient = new ActorId(Guid.NewGuid());

        var notification = new ActorCredentialsExpiring(
            recipient,
            recipient);

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(notification);
    }

    private static Task PrepareActorActivatedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new ActorMarketRole(EicFunction.MeteredDataAdministrator),
            new ActorName(string.Empty),
            null);

        actor.Activate();
        actor.ExternalActorId = new ExternalActorId(Guid.NewGuid());

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        return domainEventRepository.EnqueueAsync(actor);
    }

    private static Task PrepareActorCertificateCredentialsAssignedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new ActorMarketRole(EicFunction.EnergySupplier),
            new ActorName(string.Empty),
            null);

        actor.Credentials = new ActorCertificateCredentials(
            new string('A', 40),
            "mocked_identifier",
            DateTime.UtcNow.AddYears(1).ToInstant());

        actor.Activate();

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        return domainEventRepository.EnqueueAsync(actor);
    }

    private static Task PrepareActorCertificateCredentialsRemovedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new ActorMarketRole(EicFunction.EnergySupplier),
            new ActorName(string.Empty),
            null);

        actor.Credentials = new ActorCertificateCredentials(
            new string('A', 40),
            "mocked_identifier",
            DateTime.UtcNow.AddYears(1).ToInstant());

        actor.Activate();
        ((IPublishDomainEvents)actor).DomainEvents.ClearPublishedDomainEvents();

        actor.Credentials = null;

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        return domainEventRepository.EnqueueAsync(actor);
    }

    private static async Task PrepareScheduleActorConsolidationEventAsync(IServiceProvider scope)
    {
        var fromActorId = new ActorId(Guid.NewGuid());
        var fromActorNumber = new MockedGln();
        var scheduledAt = DateTimeOffset.UtcNow.AddMonths(2).ToInstant();

        var notification = new ActorConsolidationScheduled(
            fromActorId,
            fromActorNumber,
            scheduledAt);

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(notification);
    }

    private async Task PrepareGridAreaOwnershipAssignedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new ActorMarketRole(EicFunction.BillingAgent),
            new ActorName(string.Empty),
            null);

        var gridArea = await _fixture.PrepareGridAreaAsync();

        actor.UpdateMarketRole(new ActorMarketRole(EicFunction.GridAccessProvider, new[]
        {
            new ActorGridArea(new GridAreaId(gridArea.Id), Array.Empty<MeteringPointType>())
        }));

        actor.Activate();

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(actor);
    }

    private async Task PrepareProcessDelegationConfiguredEventAsync(IServiceProvider scope)
    {
        var actorA = await _fixture.PrepareActorAsync();
        var actorB = await _fixture.PrepareActorAsync();
        var gridArea = await _fixture.PrepareGridAreaAsync();

        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var processDelegationEntity = new ProcessDelegationEntity
        {
            DelegatedByActorId = actorA.Id,
            ConcurrencyToken = Guid.NewGuid(),
            DelegatedProcess = DelegatedProcess.ReceiveEnergyResults
        };

        await context.ProcessDelegations.AddAsync(processDelegationEntity);
        await context.SaveChangesAsync();

        var processDelegation = await scope
            .GetRequiredService<IProcessDelegationRepository>()
            .GetAsync(new ProcessDelegationId(processDelegationEntity.Id));

        processDelegation!.DelegateTo(
            new ActorId(actorB.Id),
            new GridAreaId(gridArea.Id),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(processDelegation);
    }

    private Task PrepareDomainEventAsync(IServiceProvider scope, Type domainEvent)
    {
        return domainEvent.Name switch
        {
            nameof(ActorActivated) => PrepareActorActivatedEventAsync(scope),
            nameof(ActorCertificateCredentialsAssigned) => PrepareActorCertificateCredentialsAssignedEventAsync(scope),
            nameof(ActorCertificateCredentialsRemoved) => PrepareActorCertificateCredentialsRemovedEventAsync(scope),
            nameof(GridAreaOwnershipAssigned) => PrepareGridAreaOwnershipAssignedEventAsync(scope),
            nameof(ProcessDelegationConfigured) => PrepareProcessDelegationConfiguredEventAsync(scope),
            nameof(BalanceResponsibilityValidationFailed) => PrepareBalanceResponsibilityValidationFailedEventAsync(scope),
            nameof(NewBalanceResponsibilityReceived) => PrepareNewBalanceResponsibilityReceivedEventAsync(scope),
            nameof(ActorCredentialsExpiring) => PrepareActorCredentialsExpiringEventAsync(scope),
            nameof(ActorConsolidationScheduled) => PrepareScheduleActorConsolidationEventAsync(scope),
            _ => throw new NotSupportedException($"Domain event {domainEvent.Name} is missing a test.")
        };
    }
}
