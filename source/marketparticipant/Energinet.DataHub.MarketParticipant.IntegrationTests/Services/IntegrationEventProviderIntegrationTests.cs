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
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class IntegrationEventProviderIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public IntegrationEventProviderIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
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
            .Where(domainEventType => domainEventType.IsSubclassOf(typeof(DomainEvent)))
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
            Assert.Equal(eventName, integrationEvents[0].EventName);
        }
    }

    private static async Task PrepareActorActivatedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new[] { new ActorMarketRole(EicFunction.MeteredDataAdministrator) },
            new ActorName(string.Empty),
            null);

        actor.Activate();
        actor.ExternalActorId = new ExternalActorId(Guid.NewGuid());

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(actor);
    }

    private static async Task PrepareActorCertificateCredentialsAssignedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new[] { new ActorMarketRole(EicFunction.EnergySupplier) },
            new ActorName(string.Empty),
            null);

        actor.Credentials = new ActorCertificateCredentials(new string('A', 40), "mocked_identifier");
        actor.Activate();

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(actor);
    }

    private async Task PrepareGridAreaOwnershipAssignedEventAsync(IServiceProvider scope)
    {
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            Array.Empty<ActorMarketRole>(),
            new ActorName(string.Empty),
            null);

        var gridArea = await _fixture.PrepareGridAreaAsync();

        actor.AddMarketRole(new ActorMarketRole(EicFunction.GridAccessProvider, new[]
        {
            new ActorGridArea(new GridAreaId(gridArea.Id), Array.Empty<MeteringPointType>())
        }));

        actor.Activate();

        var domainEventRepository = scope.GetRequiredService<IDomainEventRepository>();
        await domainEventRepository.EnqueueAsync(actor);
    }

    private Task PrepareDomainEventAsync(IServiceProvider scope, Type domainEvent)
    {
        return domainEvent.Name switch
        {
            nameof(ActorActivated) => PrepareActorActivatedEventAsync(scope),
            nameof(ActorCertificateCredentialsAssigned) => PrepareActorCertificateCredentialsAssignedEventAsync(scope),
            nameof(GridAreaOwnershipAssigned) => PrepareGridAreaOwnershipAssignedEventAsync(scope),
            _ => throw new NotSupportedException($"Domain event {domainEvent.Name} is missing a test.")
        };
    }
}