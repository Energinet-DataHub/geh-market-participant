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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class StopMessageDelegationHandlerTests
{
    [Fact]
    public async Task Handle_StopMessageDelegation_CorrectDoesNotThrow()
    {
        // Arrange
        var messageRepo = new Mock<IMessageDelegationRepository>();
        var target = new StopMessageDelegationHandler(messageRepo.Object);

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            actorFrom.Id,
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var messageDelegation = new MessageDelegation(
            new MessageDelegationId(Guid.NewGuid()),
            actorFrom.Id,
            DelegationMessageType.Rsm012Inbound,
            Guid.NewGuid(),
            new List<DelegationPeriod>() { delegationPeriod });
        messageRepo
            .Setup(x => x.GetAsync(It.Is<MessageDelegationId>(match => match.Value == messageDelegation.Id.Value)))
            .ReturnsAsync(messageDelegation);

        var command = new StopMessageDelegationCommand(new StopMessageDelegationDto(
            messageDelegation.Id.Value,
            delegationPeriod.Id.Value,
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_StopMessageDelegation_IncorrectDelegationIdThrows()
    {
        // Arrange
        var messageRepo = new Mock<IMessageDelegationRepository>();
        var target = new StopMessageDelegationHandler(messageRepo.Object);

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            actorFrom.Id,
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var messageDelegation = new MessageDelegation(
            new MessageDelegationId(Guid.NewGuid()),
            actorFrom.Id,
            DelegationMessageType.Rsm012Inbound,
            Guid.NewGuid(),
            new List<DelegationPeriod>() { delegationPeriod });
        messageRepo
            .Setup(x => x.GetAsync(It.Is<MessageDelegationId>(match => match.Value == messageDelegation.Id.Value)))
            .ReturnsAsync(messageDelegation);

        var testDelegationId = new MessageDelegationId(Guid.NewGuid());
        var command = new StopMessageDelegationCommand(new StopMessageDelegationDto(
            testDelegationId.Value,
            delegationPeriod.Id.Value,
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<NotFoundValidationException>(exception);
        Assert.Contains(
            $"Entity '{testDelegationId.Value}' does not exist",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_StopMessageDelegation_IncorrectPeriodIdThrows()
    {
        // Arrange
        var messageRepo = new Mock<IMessageDelegationRepository>();
        var target = new StopMessageDelegationHandler(messageRepo.Object);

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            actorFrom.Id,
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var messageDelegation = new MessageDelegation(
            new MessageDelegationId(Guid.NewGuid()),
            actorFrom.Id,
            DelegationMessageType.Rsm012Inbound,
            Guid.NewGuid(),
            new List<DelegationPeriod>() { delegationPeriod });
        messageRepo
            .Setup(x => x.GetAsync(It.Is<MessageDelegationId>(match => match.Value == messageDelegation.Id.Value)))
            .ReturnsAsync(messageDelegation);

        var testPeriodId = new DelegationPeriodId(Guid.NewGuid());
        var command = new StopMessageDelegationCommand(new StopMessageDelegationDto(
            messageDelegation.Id.Value,
            testPeriodId.Value,
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<NotFoundValidationException>(exception);
        Assert.Contains(
            $"Entity '{testPeriodId.Value}' does not exist",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}
