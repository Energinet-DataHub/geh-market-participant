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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Energinet.DataHub.MarketParticipant.Tests.Handlers;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class ActorFactoryServiceTests
{
    private readonly Address _validAddress = new(
        "test Street",
        "1",
        "1111",
        "Test City",
        "DK");

    private readonly BusinessRegisterIdentifier _validCvrBusinessRegisterIdentifier = new("12345678");
    private readonly OrganizationDomain _validDomain = new("energinet.dk");

    [Fact]
    public async Task CreateAsync_NewActor_AddsAndReturnsActor()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();

        var target = new ActorFactoryService(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IOverlappingEicFunctionsRuleService>().Object,
            new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
            new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
            new Mock<IDomainEventRepository>().Object,
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, [_validDomain]);
        var marketRole = new ActorMarketRole(EicFunction.EnergySupplier, []);
        var actorId = new ActorId(Guid.NewGuid());

        actorRepositoryMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Actor>()))
            .ReturnsAsync(new Result<ActorId, ActorError>(actorId));

        var committedActor = new Actor(
            actorId,
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            marketRole,
            new ActorName("fake_value"),
            null);

        actorRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(committedActor);

        // Act
        var response = await target
            .CreateAsync(
                organization,
                committedActor.ActorNumber,
                committedActor.Name,
                marketRole);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CreateAsync_NewActorGridAccessProvider_MultiGridAreas_AddsAndReturnsActor()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();

        var target = new ActorFactoryService(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IOverlappingEicFunctionsRuleService>().Object,
            new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
            new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
            new Mock<IDomainEventRepository>().Object,
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var validMeteringPointTypes = new List<MeteringPointType>
        {
            MeteringPointType.D05NetProduction
        };
        var validGridAreas = new List<ActorGridArea>
        {
            new(new GridAreaId(Guid.NewGuid()), validMeteringPointTypes),
            new(new GridAreaId(Guid.NewGuid()), validMeteringPointTypes),
            new(new GridAreaId(Guid.NewGuid()), validMeteringPointTypes)
        };

        var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, [_validDomain]);
        var marketRole = new ActorMarketRole(EicFunction.GridAccessProvider, validGridAreas);
        var actorId = new ActorId(Guid.NewGuid());

        actorRepositoryMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Actor>()))
            .ReturnsAsync(new Result<ActorId, ActorError>(actorId));

        var committedActor = new Actor(
            actorId,
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            marketRole,
            new ActorName("fake_value"),
            null);

        actorRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(committedActor);

        // Act
        var response = await target
            .CreateAsync(
                organization,
                committedActor.ActorNumber,
                committedActor.Name,
                marketRole);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CreateAsync_NewActor_EnsuresGlnUniqueness()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var globalLocationNumberUniquenessService = new Mock<IUniqueGlobalLocationNumberRuleService>();
        var target = new ActorFactoryService(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IOverlappingEicFunctionsRuleService>().Object,
            globalLocationNumberUniquenessService.Object,
            new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
            new Mock<IDomainEventRepository>().Object,
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, [_validDomain]);
        var marketRole = new ActorMarketRole(EicFunction.EnergySupplier, []);

        var committedActor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            marketRole,
            new ActorName("fake_value"),
            null);

        actorRepositoryMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Actor>()))
            .ReturnsAsync(new Result<ActorId, ActorError>(new ActorId(Guid.NewGuid())));
        actorRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(committedActor);

        // Act
        await target
            .CreateAsync(
                organization,
                committedActor.ActorNumber,
                committedActor.Name,
                marketRole);

        // Assert
        globalLocationNumberUniquenessService.Verify(
            x => x.ValidateGlobalLocationNumberAvailableAsync(organization, committedActor.ActorNumber),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NewActor_EnsuresValidRoles()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var overlappingBusinessRolesService = new Mock<IOverlappingEicFunctionsRuleService>();
        var target = new ActorFactoryService(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            overlappingBusinessRolesService.Object,
            new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
            new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
            new Mock<IDomainEventRepository>().Object,
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, [_validDomain]);
        var meteringPointTypes = new[]
        {
            MeteringPointType.D02Analysis
        };
        var gridAreas = new List<ActorGridArea>
        {
            new(meteringPointTypes)
        };
        var marketRole = new ActorMarketRole(EicFunction.BalanceResponsibleParty, gridAreas);

        var committedActor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            marketRole,
            new ActorName("fake_value"),
            null);

        actorRepositoryMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Actor>()))
            .ReturnsAsync(new Result<ActorId, ActorError>(new ActorId(Guid.NewGuid())));
        actorRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(committedActor);

        // Act
        await target
            .CreateAsync(
                organization,
                committedActor.ActorNumber,
                committedActor.Name,
                marketRole);

        // Assert
        overlappingBusinessRolesService.Verify(
            x => x.ValidateEicFunctionsAcrossActorsAsync(It.IsAny<Actor>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NewActor_EnsuresUniqueGridAreas()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var uniqueMarketRoleGridAreaRuleService = new Mock<IUniqueMarketRoleGridAreaRuleService>();
        var target = new ActorFactoryService(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IOverlappingEicFunctionsRuleService>().Object,
            new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
            uniqueMarketRoleGridAreaRuleService.Object,
            new Mock<IDomainEventRepository>().Object,
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, [_validDomain]);
        var meteringPointTypes = new[]
        {
            MeteringPointType.D02Analysis
        };
        var gridAreas = new List<ActorGridArea>
        {
            new(new GridAreaId(Guid.NewGuid()), meteringPointTypes)
        };
        var marketRole = new ActorMarketRole(EicFunction.BalanceResponsibleParty, gridAreas);

        var committedActor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            marketRole,
            new ActorName("fake_value"),
            null);

        actorRepositoryMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Actor>()))
            .ReturnsAsync(new Result<ActorId, ActorError>(new ActorId(Guid.NewGuid())));
        actorRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(committedActor);

        // Act
        await target
            .CreateAsync(
                organization,
                committedActor.ActorNumber,
                committedActor.Name,
                marketRole);

        // Assert
        uniqueMarketRoleGridAreaRuleService.Verify(
            x => x.ValidateAndReserveAsync(committedActor),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NewActor_EnsuresEventsAreEnqueued()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var domainEventRepositoryMock = new Mock<IDomainEventRepository>();
        var target = new ActorFactoryService(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IOverlappingEicFunctionsRuleService>().Object,
            new Mock<IUniqueGlobalLocationNumberRuleService>().Object,
            new Mock<IUniqueMarketRoleGridAreaRuleService>().Object,
            domainEventRepositoryMock.Object,
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var organization = new Organization("fake_value", _validCvrBusinessRegisterIdentifier, _validAddress, [_validDomain]);
        var meteringPointTypes = new[]
        {
            MeteringPointType.D02Analysis
        };
        var gridAreas = new List<ActorGridArea>
        {
            new(new GridAreaId(Guid.NewGuid()), meteringPointTypes)
        };
        var marketRole = new ActorMarketRole(EicFunction.BalanceResponsibleParty, gridAreas);

        var committedActor = new Actor(
            new ActorId(Guid.NewGuid()),
            organization.Id,
            null,
            new MockedGln(),
            ActorStatus.Active,
            marketRole,
            new ActorName("fake_value"),
            null);

        actorRepositoryMock
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Actor>()))
            .ReturnsAsync(new Result<ActorId, ActorError>(new ActorId(Guid.NewGuid())));
        actorRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(committedActor);

        // Act
        await target.CreateAsync(
            organization,
            committedActor.ActorNumber,
            committedActor.Name,
            marketRole);

        // Assert
        domainEventRepositoryMock.Verify(x => x.EnqueueAsync(committedActor), Times.Once);
    }
}
