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

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateMessageDelegationHandlerTests
    {
        [Fact]
        public async Task Handle_NewMessageDelegation_CorrectDoesNotThrow()
        {
            // Arrange
            var actorRepo = new Mock<IActorRepository>();
            var target = new CreateMessageDelegationHandler(
                actorRepo.Object,
                new Mock<IMessageDelegationRepository>().Object,
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

            var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
                actorFrom.Id,
                actorTo.Id,
                new List<GridAreaId> { new(Guid.NewGuid()) },
                new List<DelegationMessageType> { DelegationMessageType.Rsm012Inbound },
                DateTimeOffset.UtcNow));

            // Act
            var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task Handle_NewMessageDelegation_InActiveActorFromThrows()
        {
            // Arrange
            var actorRepo = new Mock<IActorRepository>();
            var target = new CreateMessageDelegationHandler(
                actorRepo.Object,
                new Mock<IMessageDelegationRepository>().Object,
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
                actorFrom.Id,
                actorTo.Id,
                new List<GridAreaId> { new(Guid.NewGuid()) },
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
                actorFrom.Id,
                actorTo.Id,
                new List<GridAreaId> { new(Guid.NewGuid()) },
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
                UnitOfWorkProviderMock.Create(),
                new Mock<IEntityLock>().Object,
                new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

            var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
            var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

            actorRepo
                .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorTo.Id.Value)))
                .ReturnsAsync(actorTo);

            var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
                actorFrom.Id,
                actorTo.Id,
                new List<GridAreaId> { new(Guid.NewGuid()) },
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
                UnitOfWorkProviderMock.Create(),
                new Mock<IEntityLock>().Object,
                new Mock<IAllowedMarketRoleCombinationsForDelegationRuleService>().Object);

            var actorFrom = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());
            var actorTo = TestPreparationModels.MockedActiveActor(Guid.NewGuid(), Guid.NewGuid());

            actorRepo
                .Setup(x => x.GetAsync(It.Is<ActorId>(match => match.Value == actorFrom.Id.Value)))
                .ReturnsAsync(actorFrom);

            var command = new CreateMessageDelegationCommand(new CreateMessageDelegationDto(
                actorFrom.Id,
                actorTo.Id,
                new List<GridAreaId> { new(Guid.NewGuid()) },
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
    }
}
