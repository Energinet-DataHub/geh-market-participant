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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class CreateMessageDelegationHandlerTests
{
    [Fact]
    public async Task Handle_NewMessageDelegation_CorrectDoesNotThrow()
    {
        // Arrange
        var actorRepo = new Mock<IActorRepository>();
        var messageDelegationRepository = new Mock<IMessageDelegationRepository>();
        var target = new CreateMessageDelegationHandler(
            actorRepo.Object,
            messageDelegationRepository.Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
        var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorFrom.Id.Value)))
            .ReturnsAsync(actorFrom);
        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorTo.Id.Value)))
            .ReturnsAsync(actorTo);

        var messageDelegations = new Dictionary<MessageDelegationId, MessageDelegation>();

        messageDelegationRepository
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<MessageDelegation>()))
            .ReturnsAsync(new Func<MessageDelegation, MessageDelegationId>(md =>
            {
                var newId = new MessageDelegationId(Guid.NewGuid());
                messageDelegations.Add(newId, md);
                return newId;
            }));

        messageDelegationRepository
            .Setup(x => x.GetAsync(It.IsAny<MessageDelegationId>()))
            .ReturnsAsync(new Func<MessageDelegationId, MessageDelegation?>(id => messageDelegations.GetValueOrDefault(id)));

        var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
            actorFrom.Id.Value,
            actorTo.Id.Value,
            new List<Guid> { Guid.NewGuid() },
            new List<DelegationMessageType> { DelegationMessageType.Rsm012Inbound },
            DateTimeOffset.UtcNow));

        // Act + Assert
        await target.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_NewMessageDelegation_InActiveActorFromThrows()
    {
        // Arrange
        var actorRepo = new Mock<IActorRepository>();
        var target = new CreateMessageDelegationHandler(
            actorRepo.Object,
            new Mock<IMessageDelegationRepository>().Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var actorFrom = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorFrom.Id.Value)))
            .ReturnsAsync(actorFrom);
        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorTo.Id.Value)))
            .ReturnsAsync(actorTo);

        var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
            actorFrom.Id.Value,
            actorTo.Id.Value,
            new List<Guid> { Guid.NewGuid() },
            new List<DelegationMessageType> { DelegationMessageType.Rsm012Inbound },
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ValidationException>(exception);
        Assert.Contains(
            $"Actors to delegate from/to must both be active to delegate messages",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NewMessageDelegation_InActiveActorToThrows()
    {
        // Arrange
        var actorRepo = new Mock<IActorRepository>();
        var target = new CreateMessageDelegationHandler(
            actorRepo.Object,
            new Mock<IMessageDelegationRepository>().Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
        var actorTo = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());

        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorFrom.Id.Value)))
            .ReturnsAsync(actorFrom);
        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorTo.Id.Value)))
            .ReturnsAsync(actorTo);

        var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
            actorFrom.Id.Value,
            actorTo.Id.Value,
            new List<Guid> { Guid.NewGuid() },
            new List<DelegationMessageType> { DelegationMessageType.Rsm012Inbound },
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ValidationException>(exception);
        Assert.Contains(
            $"Actors to delegate from/to must both be active to delegate messages.",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NewMessageDelegation_UnknownActorFromThrows()
    {
        // Arrange
        var actorRepo = new Mock<IActorRepository>();
        var target = new CreateMessageDelegationHandler(
            actorRepo.Object,
            new Mock<IMessageDelegationRepository>().Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
        var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorTo.Id.Value)))
            .ReturnsAsync(actorTo);

        var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
            actorFrom.Id.Value,
            actorTo.Id.Value,
            new List<Guid> { Guid.NewGuid() },
            new List<DelegationMessageType> { DelegationMessageType.Rsm012Inbound },
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<NotFoundValidationException>(exception);
        Assert.Contains(
            $"Entity '{actorFrom.Id.Value}' does not exist.",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NewMessageDelegation_UnknownActorToThrows()
    {
        // Arrange
        var actorRepo = new Mock<IActorRepository>();
        var target = new CreateMessageDelegationHandler(
            actorRepo.Object,
            new Mock<IMessageDelegationRepository>().Object,
            new Mock<IDomainEventRepository>().Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
        var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

        actorRepo
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorFrom.Id.Value)))
            .ReturnsAsync(actorFrom);

        var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
            actorFrom.Id.Value,
            actorTo.Id.Value,
            new List<Guid> { Guid.NewGuid() },
            new List<DelegationMessageType> { DelegationMessageType.Rsm012Inbound },
            DateTimeOffset.UtcNow));

        // Act
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<NotFoundValidationException>(exception);
        Assert.Contains(
            $"Entity '{actorTo.Id.Value}' does not exist.",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NewMessageDelegation_PublishesEvents()
    {
        // Arrange
        var actorRepository = new Mock<IActorRepository>();
        var messageDelegationRepository = new Mock<IMessageDelegationRepository>();
        var domainEventRepository = new Mock<IDomainEventRepository>();
        var target = new CreateMessageDelegationHandler(
            actorRepository.Object,
            messageDelegationRepository.Object,
            domainEventRepository.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IEntityLock>().Object,
            new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

        var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
        var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

        actorRepository
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorFrom.Id.Value)))
            .ReturnsAsync(actorFrom);
        actorRepository
            .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorTo.Id.Value)))
            .ReturnsAsync(actorTo);

        var messageDelegations = new Dictionary<MessageDelegationId, MessageDelegation>();

        messageDelegationRepository
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<MessageDelegation>()))
            .ReturnsAsync(new Func<MessageDelegation, MessageDelegationId>(md =>
            {
                var newId = new MessageDelegationId(Guid.NewGuid());
                messageDelegations.Add(newId, md);
                return newId;
            }));

        messageDelegationRepository
            .Setup(x => x.GetAsync(It.IsAny<MessageDelegationId>()))
            .ReturnsAsync(new Func<MessageDelegationId, MessageDelegation?>(id => messageDelegations.GetValueOrDefault(id)));

        var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
            actorFrom.Id.Value,
            actorTo.Id.Value,
            [Guid.NewGuid()],
            [DelegationMessageType.Rsm012Inbound],
            DateTimeOffset.UtcNow));

        // Act
        await target.Handle(command, CancellationToken.None);

        // Assert
        domainEventRepository.Verify(x => x.EnqueueAsync(messageDelegations.Last().Value));
    }
}
