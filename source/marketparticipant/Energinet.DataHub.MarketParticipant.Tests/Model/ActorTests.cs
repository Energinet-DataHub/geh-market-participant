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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class ActorTests
{
    [Fact]
    public void Ctor_NewRole_HasStatusNew()
    {
        // Arrange + Act
        var actor = new Actor(new OrganizationId(Guid.NewGuid()), new MockedGln(), new ActorName("Mock"), new ActorMarketRole(EicFunction.BillingAgent));

        // Assert
        Assert.Equal(ActorStatus.New, actor.Status);
    }

    [Fact]
    public void Activate_WithoutId_NotAllowed()
    {
        // Arrange
        var target = new Actor(new OrganizationId(Guid.NewGuid()), new MockedGln(), new ActorName("fake_value"), new ActorMarketRole(EicFunction.BillingAgent));

        // Act + Assert
        Assert.Throws<NotSupportedException>(() => target.Activate());
    }

    [Theory]
    [InlineData(ActorStatus.New, true)]
    [InlineData(ActorStatus.Active, true)]
    public void Activate_ChangesState_IfAllowed(ActorStatus initialStatus, bool isAllowed)
    {
        // Arrange
        var target = CreateTestActor(initialStatus);

        // Act + Assert
        if (isAllowed)
        {
            target.Activate();
            Assert.Equal(ActorStatus.Active, target.Status);
        }
        else
        {
            Assert.Throws<ValidationException>(() => target.Activate());
        }
    }

    [Theory]
    [InlineData(ActorStatus.Active, true)]
    [InlineData(ActorStatus.Inactive, true)]
    [InlineData(ActorStatus.Passive, true)]
    public void Deactivate_ChangesState_IfAllowed(ActorStatus initialStatus, bool isAllowed)
    {
        // Arrange
        var target = CreateTestActor(initialStatus);

        // Act + Assert
        if (isAllowed)
        {
            target.Deactivate();
            Assert.Equal(ActorStatus.Inactive, target.Status);
        }
        else
        {
            Assert.Throws<ValidationException>(() => target.Deactivate());
        }
    }

    [Theory]
    [InlineData(ActorStatus.Active, true)]
    [InlineData(ActorStatus.Passive, true)]
    public void SetAsPassive_ChangesState_IfAllowed(ActorStatus initialStatus, bool isAllowed)
    {
        // Arrange
        var target = CreateTestActor(initialStatus);

        // Act + Assert
        if (isAllowed)
        {
            target.SetAsPassive();
            Assert.Equal(ActorStatus.Passive, target.Status);
        }
        else
        {
            Assert.Throws<ValidationException>(() => target.SetAsPassive());
        }
    }

    [Fact]
    public void AddMarketRole_AddsNewRole_IsAllowed()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.New);
        var role = new ActorMarketRole(EicFunction.GridAccessProvider, Array.Empty<ActorGridArea>());

        // Act
        target.UpdateMarketRole(role);

        // Assert
        Assert.Equal(role, target.MarketRole);
    }

    [Fact]
    public void AddMarketRole_StatusNotNew_NotAllowed()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.Active);
        var role = new ActorMarketRole(EicFunction.GridAccessProvider, Array.Empty<ActorGridArea>());

        // Act + Assert
        Assert.Throws<ValidationException>(() => target.UpdateMarketRole(role));
    }

    [Fact]
    public void Activate_WithMarketRoles_PublishesEvents()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.New);
        var gridAreas = new[]
        {
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), Array.Empty<MeteringPointType>()),
        };

        target.UpdateMarketRole(new ActorMarketRole(EicFunction.GridAccessProvider, gridAreas));

        // Act
        target.Activate();

        // Assert
        Assert.Equal(1, ((IPublishDomainEvents)target).DomainEvents.Count(e => e is GridAreaOwnershipAssigned));
    }

    [Fact]
    public void ExternalActorId_IsAssigned_PublishesEvents()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.Active, EicFunction.BalanceResponsibleParty);

        // Act
        target.ExternalActorId = new ExternalActorId(Guid.NewGuid());

        // Assert
        Assert.Equal(1, ((IPublishDomainEvents)target).DomainEvents.Count(e => e is ActorActivated));
    }

    [Fact]
    public void Activate_WithCredentials_PublishesEvents()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.New, EicFunction.EnergySupplier);
        target.Credentials = new ActorCertificateCredentials(
            new string('A', 40),
            "mocked_identifier",
            DateTime.UtcNow.AddYears(1).ToInstant());

        // Act
        target.Activate();

        // Assert
        Assert.Equal(1, ((IPublishDomainEvents)target).DomainEvents.Count(e => e is ActorCertificateCredentialsAssigned));
    }

    [Fact]
    public void Credentials_AreAssigned_PublishesEvents()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.Active, EicFunction.EnergySupplier);

        // Act
        target.Credentials = new ActorCertificateCredentials(
            new string('A', 40),
            "mocked_identifier",
            DateTime.UtcNow.AddYears(1).ToInstant());

        // Assert
        Assert.Equal(1, ((IPublishDomainEvents)target).DomainEvents.Count(e => e is ActorCertificateCredentialsAssigned));
    }

    [Fact]
    public void Credentials_AreRemoved_PublishesEvents()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.Active, EicFunction.EnergySupplier);
        target.Credentials = new ActorCertificateCredentials(
            new string('A', 40),
            "mocked_identifier",
            DateTime.UtcNow.AddYears(1).ToInstant());

        // Act
        target.Credentials = null;

        // Assert
        Assert.Equal(1, ((IPublishDomainEvents)target).DomainEvents.Count(e => e is ActorCertificateCredentialsAssigned));
        Assert.Equal(1, ((IPublishDomainEvents)target).DomainEvents.Count(e => e is ActorCertificateCredentialsRemoved));
    }

    [Fact]
    public void Deactivate_HasCredentials_NotAllowed()
    {
        // Arrange
        var target = CreateTestActor(ActorStatus.Active);
        target.Credentials = new ActorClientSecretCredentials(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.ToInstant());

        // Act + Assert
        Assert.Throws<ValidationException>(() => target.Deactivate());
    }

    private static Actor CreateTestActor(ActorStatus status, EicFunction eicFunction = EicFunction.BillingAgent)
    {
        return new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            new ExternalActorId(Guid.Empty),
            new MockedGln(),
            status,
            new ActorMarketRole(eicFunction),
            new ActorName("test_actor_name"),
            null);
    }
}
