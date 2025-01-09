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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class ScheduleActorConsolidationsHandlerTests
{
    [Fact]
    public async Task Handle_InvalidFromActorId_ThrowsException()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var fromActorId = Guid.NewGuid();
        var target = new ScheduleConsolidateActorsHandler(
            Mock.Of<IAuditIdentityProvider>(),
            Mock.Of<IActorConsolidationAuditLogRepository>(),
            Mock.Of<IActorConsolidationRepository>(),
            Mock.Of<IDomainEventRepository>(),
            Mock.Of<IUnitOfWorkProvider>(),
            actorRepositoryMock.Object,
            Mock.Of<IExistingActorConsolidationService>());
        var command = new ScheduleConsolidateActorsCommand(fromActorId, new ConsolidationRequestDto(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(65)));

        // Act + Assert
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));
        Assert.NotNull(exception);
        Assert.Equal(typeof(NotFoundValidationException), exception.GetType());
        Assert.Contains(exception.Message, $"Entity '{fromActorId}' does not exist.", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_InvalidToActorId_ThrowsException()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var validFromActor = TestPreparationModels.MockedActor();
        var toActorId = Guid.NewGuid();
        var target = new ScheduleConsolidateActorsHandler(
            Mock.Of<IAuditIdentityProvider>(),
            Mock.Of<IActorConsolidationAuditLogRepository>(),
            Mock.Of<IActorConsolidationRepository>(),
            Mock.Of<IDomainEventRepository>(),
            Mock.Of<IUnitOfWorkProvider>(),
            actorRepositoryMock.Object,
            Mock.Of<IExistingActorConsolidationService>());
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(validFromActor.Id))
            .ReturnsAsync(validFromActor);

        var command = new ScheduleConsolidateActorsCommand(validFromActor.Id.Value, new ConsolidationRequestDto(toActorId, DateTimeOffset.UtcNow.AddDays(65)));

        // Act + Assert
        var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));
        Assert.NotNull(exception);
        Assert.Equal(typeof(NotFoundValidationException), exception.GetType());
        Assert.Contains(exception.Message, $"Entity '{toActorId}' does not exist.", StringComparison.OrdinalIgnoreCase);
    }
}
