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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class ExternalActorIdConfigurationServiceTests
{
    [Theory]
    [InlineData(new[] { ActorStatus.New }, true)]
    [InlineData(new[] { ActorStatus.Active }, false)]
    [InlineData(new[] { ActorStatus.Active, ActorStatus.Passive }, false)]
    [InlineData(new[] { ActorStatus.Active, ActorStatus.Inactive }, true)]
    public async Task AssignExternalActorIdAsync_HasExternalActorId_RemovesId(ActorStatus[] status, bool shouldDelete)
    {
        // Arrange
        var activeDirectoryService = new Mock<IActiveDirectoryB2CService>();
        var target = new ExternalActorIdConfigurationService(activeDirectoryService.Object);

        activeDirectoryService.Setup(x => x.AssignApplicationRegistrationAsync(It.IsAny<Actor>()))
            .Callback<Actor>(x => x.ExternalActorId = new ExternalActorId(Guid.NewGuid()));
        activeDirectoryService.Setup(x => x.DeleteAppRegistrationAsync(It.IsAny<Actor>()))
            .Callback<Actor>(x => x.ExternalActorId = null);

        var gln = new MockedGln();
        var externalActorId = new ExternalActorId(Guid.NewGuid());
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            externalActorId,
            gln,
            ActorStatus.New,
            new ActorMarketRole(EicFunction.BillingAgent),
            new ActorName("fake_value"),
            null);

        foreach (var s in status)
        {
            actor.Status = s;
        }

        // Act
        await target.AssignExternalActorIdAsync(actor);

        // Assert
        if (shouldDelete)
        {
            Assert.Null(actor.ExternalActorId);
            activeDirectoryService.Verify(
                x => x.DeleteAppRegistrationAsync(actor),
                Times.Once);
        }
        else
        {
            Assert.Equal(externalActorId, actor.ExternalActorId);
            activeDirectoryService.Verify(
                x => x.DeleteAppRegistrationAsync(actor),
                Times.Never);
        }

        activeDirectoryService.Verify(
            x => x.AssignApplicationRegistrationAsync(actor),
            Times.Never);
    }

    [Theory]
    [InlineData(new[] { ActorStatus.New }, false)]
    [InlineData(new[] { ActorStatus.Active }, true)]
    [InlineData(new[] { ActorStatus.Active, ActorStatus.Passive }, true)]
    [InlineData(new[] { ActorStatus.Active, ActorStatus.Inactive }, false)]
    public async Task AssignExternalActorIdAsync_HasNoExternalActorId_CreatesId(ActorStatus[] status, bool shouldCreate)
    {
        // Arrange
        var activeDirectoryService = new Mock<IActiveDirectoryB2CService>();
        var target = new ExternalActorIdConfigurationService(activeDirectoryService.Object);

        activeDirectoryService.Setup(x => x.AssignApplicationRegistrationAsync(It.IsAny<Actor>()))
            .Callback<Actor>(x => x.ExternalActorId = new ExternalActorId(Guid.NewGuid()));
        activeDirectoryService.Setup(x => x.DeleteAppRegistrationAsync(It.IsAny<Actor>()))
            .Callback<Actor>(x => x.ExternalActorId = null);

        var gln = new MockedGln();
        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            gln,
            ActorStatus.New,
            new ActorMarketRole(EicFunction.BillingAgent),
            new ActorName("fake_value"),
            null);

        foreach (var s in status)
        {
            actor.Status = s;
        }

        // Act
        await target.AssignExternalActorIdAsync(actor);

        // Assert
        if (shouldCreate)
        {
            Assert.NotNull(actor.ExternalActorId);
        }
        else
        {
            Assert.Null(actor.ExternalActorId);
        }

        activeDirectoryService.Verify(
            x => x.DeleteAppRegistrationAsync(actor),
            Times.Never);
    }
}
