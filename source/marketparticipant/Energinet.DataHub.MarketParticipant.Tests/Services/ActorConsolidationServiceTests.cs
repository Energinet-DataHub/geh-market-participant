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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Energinet.DataHub.MarketParticipant.Tests.Handlers;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public class ActorConsolidationServiceTests
{
    private static readonly Organization _validOrganization = new(
        new OrganizationId(Guid.NewGuid()),
        "Organization Name",
        MockedBusinessRegisterIdentifier.New(),
        new Address(null, null, null, null, "DK"),
        [new OrganizationDomain("test.datahub.dk")],
        OrganizationStatus.Active);

    [Fact]
    public async Task ConsolidateAsync_TwoGridAccessProviders_EmptyDestination_TransfersGridAreas()
    {
        // Arrange
        var validFromActor = GenerateValidActor(EicFunction.GridAccessProvider, [
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), []),
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), []),
            ]);

        var validToActor = GenerateValidActor(EicFunction.GridAccessProvider, []);

        var actorConsolidationAuditLogRepository = new Mock<IActorConsolidationAuditLogRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var auditIdentityProvider = new Mock<IAuditIdentityProvider>();
        var domainEventRepository = new Mock<IDomainEventRepository>();
        var gridAreaRepository = new Mock<IGridAreaRepository>();

        var target = new ActorConsolidationService(
            actorConsolidationAuditLogRepository.Object,
            actorRepository.Object,
            auditIdentityProvider.Object,
            domainEventRepository.Object,
            gridAreaRepository.Object);

        actorRepository
            .Setup(organizationRepository => organizationRepository.GetAsync(validFromActor.Id))
            .ReturnsAsync(validFromActor);
        actorRepository
            .Setup(organizationRepository => organizationRepository.GetAsync(validToActor.Id))
            .ReturnsAsync(validToActor);

        var actorConsolidation = new ActorConsolidation(validFromActor.Id, validToActor.Id, Instant.FromUtc(2024, 1, 1, 10, 59));

        // Act
        await target.ConsolidateAsync(actorConsolidation);

        // Assert
        Assert.Empty(validFromActor.MarketRole.GridAreas);
        Assert.Equal(ActorStatus.Inactive, validFromActor.Status);
        Assert.Equal(2, validToActor.MarketRole.GridAreas.Count);
        domainEventRepository.Verify(mock => mock.EnqueueAsync(validFromActor), Times.Exactly(1));
        domainEventRepository.Verify(mock => mock.EnqueueAsync(validToActor), Times.Exactly(1));
        actorConsolidationAuditLogRepository.Verify(
            mock => mock.AuditAsync(
                It.IsAny<AuditIdentity>(),
                GridAreaAuditedChange.ConsolidationCompleted,
                actorConsolidation,
                It.IsAny<GridAreaId>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ConsolidateAsync_TwoGridAccessProviders_NotEmptyDestination_TransfersGridAreas()
    {
        // Arrange
        var validFromActor = GenerateValidActor(EicFunction.GridAccessProvider, [
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), []),
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), []),
            ]);

        var validToActor = GenerateValidActor(EicFunction.GridAccessProvider, [
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), [])
            ]);

        var actorConsolidationAuditLogRepository = new Mock<IActorConsolidationAuditLogRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var auditIdentityProvider = new Mock<IAuditIdentityProvider>();
        var domainEventRepository = new Mock<IDomainEventRepository>();
        var gridAreaRepository = new Mock<IGridAreaRepository>();

        var target = new ActorConsolidationService(
            actorConsolidationAuditLogRepository.Object,
            actorRepository.Object,
            auditIdentityProvider.Object,
            domainEventRepository.Object,
            gridAreaRepository.Object);

        actorRepository
            .Setup(organizationRepository => organizationRepository.GetAsync(validFromActor.Id))
            .ReturnsAsync(validFromActor);
        actorRepository
            .Setup(organizationRepository => organizationRepository.GetAsync(validToActor.Id))
            .ReturnsAsync(validToActor);

        var actorConsolidation = new ActorConsolidation(validFromActor.Id, validToActor.Id, Instant.FromUtc(2024, 1, 1, 10, 59));

        // Act
        await target.ConsolidateAsync(actorConsolidation);

        // Assert
        Assert.Empty(validFromActor.MarketRole.GridAreas);
        Assert.Equal(ActorStatus.Inactive, validFromActor.Status);
        Assert.Equal(3, validToActor.MarketRole.GridAreas.Count);
        domainEventRepository.Verify(mock => mock.EnqueueAsync(validFromActor), Times.Exactly(1));
        domainEventRepository.Verify(mock => mock.EnqueueAsync(validToActor), Times.Exactly(1));
        actorConsolidationAuditLogRepository.Verify(
        mock => mock.AuditAsync(
            It.IsAny<AuditIdentity>(),
            GridAreaAuditedChange.ConsolidationCompleted,
            actorConsolidation,
            It.IsAny<GridAreaId>()),
        Times.Exactly(2));
    }

    [Fact]
    public async Task ConsolidateAsync_FromNotGridAccessProvider_NoTransfer_DeactivatesActor()
    {
        // Arrange
        var validFromActor = GenerateValidActor(EicFunction.EnergySupplier, []);

        var validToActor = GenerateValidActor(EicFunction.GridAccessProvider, [
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), [])
            ]);

        var actorConsolidationAuditLogRepository = new Mock<IActorConsolidationAuditLogRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var auditIdentityProvider = new Mock<IAuditIdentityProvider>();
        var domainEventRepository = new Mock<IDomainEventRepository>();
        var gridAreaRepository = new Mock<IGridAreaRepository>();

        var target = new ActorConsolidationService(
            actorConsolidationAuditLogRepository.Object,
            actorRepository.Object,
            auditIdentityProvider.Object,
            domainEventRepository.Object,
            gridAreaRepository.Object);

        actorRepository
            .Setup(organizationRepository => organizationRepository.GetAsync(validFromActor.Id))
            .ReturnsAsync(validFromActor);
        actorRepository
            .Setup(organizationRepository => organizationRepository.GetAsync(validToActor.Id))
            .ReturnsAsync(validToActor);

        var actorConsolidation = new ActorConsolidation(validFromActor.Id, validToActor.Id, Instant.FromUtc(2024, 1, 1, 10, 59));

        // Act
        await target.ConsolidateAsync(actorConsolidation);

        // Assert
        Assert.Equal(ActorStatus.Inactive, validFromActor.Status);
        Assert.Equal(validToActor.MarketRole.GridAreas.Count, validToActor.MarketRole.GridAreas.Count);
        domainEventRepository.Verify(mock => mock.EnqueueAsync(validFromActor), Times.Exactly(1));
        domainEventRepository.Verify(mock => mock.EnqueueAsync(validToActor), Times.Exactly(1));
        actorConsolidationAuditLogRepository.Verify(
        mock => mock.AuditAsync(
            It.IsAny<AuditIdentity>(),
            GridAreaAuditedChange.ConsolidationCompleted,
            actorConsolidation,
            It.IsAny<GridAreaId>()),
        Times.Never());
    }

    private static Actor GenerateValidActor(EicFunction eic, IEnumerable<ActorGridArea> actorGridAreas)
    {
        return new(
            new ActorId(Guid.NewGuid()),
            _validOrganization.Id,
            new ExternalActorId(Guid.NewGuid()),
            new MockedGln(),
            ActorStatus.Active,
            new ActorMarketRole(eic, actorGridAreas),
            new ActorName("Actor Name"),
            null);
    }
}
