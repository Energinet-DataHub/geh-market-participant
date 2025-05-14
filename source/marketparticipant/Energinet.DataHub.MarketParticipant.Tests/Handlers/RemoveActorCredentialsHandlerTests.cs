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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class RemoveActorCredentialsHandlerTests
{
    [Fact]
    public async Task Handle_NoActor_ThrowsNotFoundException()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var domainEventRepositoryMock = new Mock<IDomainEventRepository>();
        var actorCredentialsRemovalService = new Mock<IActorCredentialsRemovalService>();
        var target = new RemoveActorCredentialsHandler(
            actorRepositoryMock.Object,
            UnitOfWorkProviderMock.Create(),
            domainEventRepositoryMock.Object,
            actorCredentialsRemovalService.Object);

        var actorId = Guid.NewGuid();

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
            .ReturnsAsync((Actor?)null);

        var command = new RemoveActorCredentialsCommand(actorId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
    }
}
