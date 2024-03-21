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
public sealed class StopProcessDelegationHandlerTests
{
    [Fact]
    public async Task Handle_StopProcessDelegation_CorrectDoesNotThrow()
    {
        // Arrange
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        var target = new StopProcessDelegationHandler(
            processDelegationRepository.Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create());

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            actorFrom.Id,
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var processDelegation = new ProcessDelegation(
            new ProcessDelegationId(Guid.NewGuid()),
            actorFrom.Id,
            DelegatedProcess.RequestEnergyResults,
            Guid.NewGuid(),
            new List<DelegationPeriod>() { delegationPeriod });

        processDelegationRepository
            .Setup(x => x.GetAsync(It.Is<ProcessDelegationId>(match => match.Value == processDelegation.Id.Value)))
            .ReturnsAsync(processDelegation);

        var command = new StopProcessDelegationCommand(new StopProcessDelegationDto(
            processDelegation.Id.Value,
            delegationPeriod.Id.Value,
            DateTimeOffset.UtcNow));

        // Act + Assert
        await target.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_StopProcessDelegation_IncorrectDelegationIdThrows()
    {
        // Arrange
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        var target = new StopProcessDelegationHandler(
            processDelegationRepository.Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create());

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            actorFrom.Id,
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var processDelegation = new ProcessDelegation(
            new ProcessDelegationId(Guid.NewGuid()),
            actorFrom.Id,
            DelegatedProcess.RequestEnergyResults,
            Guid.NewGuid(),
            new List<DelegationPeriod>() { delegationPeriod });

        processDelegationRepository
            .Setup(x => x.GetAsync(It.Is<ProcessDelegationId>(match => match.Value == processDelegation.Id.Value)))
            .ReturnsAsync(processDelegation);

        var testDelegationId = new ProcessDelegationId(Guid.NewGuid());
        var command = new StopProcessDelegationCommand(new StopProcessDelegationDto(
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
    public async Task Handle_StopProcessDelegation_IncorrectPeriodIdThrows()
    {
        // Arrange
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        var target = new StopProcessDelegationHandler(
            processDelegationRepository.Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create());

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            actorFrom.Id,
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var processDelegation = new ProcessDelegation(
            new ProcessDelegationId(Guid.NewGuid()),
            actorFrom.Id,
            DelegatedProcess.RequestEnergyResults,
            Guid.NewGuid(),
            new List<DelegationPeriod>() { delegationPeriod });

        processDelegationRepository
            .Setup(x => x.GetAsync(It.Is<ProcessDelegationId>(match => match.Value == processDelegation.Id.Value)))
            .ReturnsAsync(processDelegation);

        var testPeriodId = new DelegationPeriodId(Guid.NewGuid());
        var command = new StopProcessDelegationCommand(new StopProcessDelegationDto(
            processDelegation.Id.Value,
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

    [Fact]
    public async Task Handle_StopProcessDelegation_PublishesEvents()
    {
        // Arrange
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        var domainEventRepository = new Mock<IDomainEventRepository>();
        var target = new StopProcessDelegationHandler(
            processDelegationRepository.Object,
            domainEventRepository.Object,
            UnitOfWorkProviderMock.Create());

        var delegationPeriod = new DelegationPeriod(
            new DelegationPeriodId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            new GridAreaId(Guid.NewGuid()),
            Instant.FromDateTimeOffset(DateTimeOffset.UtcNow),
            null);

        var processDelegation = new ProcessDelegation(
            new ProcessDelegationId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            DelegatedProcess.RequestEnergyResults,
            Guid.NewGuid(),
            [delegationPeriod]);

        processDelegationRepository
            .Setup(x => x.GetAsync(It.Is<ProcessDelegationId>(match => match.Value == processDelegation.Id.Value)))
            .ReturnsAsync(processDelegation);

        var command = new StopProcessDelegationCommand(new StopProcessDelegationDto(
            processDelegation.Id.Value,
            delegationPeriod.Id.Value,
            DateTimeOffset.UtcNow));

        // Act
        await target.Handle(command, CancellationToken.None);

        // Assert
        domainEventRepository.Verify(x => x.EnqueueAsync(processDelegation));
    }
}
