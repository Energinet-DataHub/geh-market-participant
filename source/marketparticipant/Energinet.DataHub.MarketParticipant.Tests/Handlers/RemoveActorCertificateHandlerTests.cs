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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class RemoveActorCertificateHandlerTests
    {
        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var certificateServiceMock = new Mock<ICertificateService>();
            var target = new RemoveActorCertificateHandler(actorRepositoryMock.Object, certificateServiceMock.Object);

            var actorId = Guid.NewGuid();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
                .ReturnsAsync((Actor?)null);

            var command = new RemoveActorCertificateCommand(actorId);

            // Act + Assert
            await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ActorHasNoCertificateCredentials_ReturnsOk()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var certificateServiceMock = new Mock<ICertificateService>();
            var target = new RemoveActorCertificateHandler(actorRepositoryMock.Object, certificateServiceMock.Object);

            var actorId = Guid.NewGuid();
            var actor = TestPreparationModels.MockedActor(actorId);

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var command = new RemoveActorCertificateCommand(actorId);

            // Act
            var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task Handle_ActorHasValidCertificateCredentials_ReturnsOk()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var certificateServiceMock = new Mock<ICertificateService>();
            var target = new RemoveActorCertificateHandler(actorRepositoryMock.Object, certificateServiceMock.Object);

            var actorId = Guid.NewGuid();
            var actor = TestPreparationModels.MockedActor(actorId);
            actor.Credentials = new ActorCertificateCredentials("mocked", "mocked");

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var command = new RemoveActorCertificateCommand(actorId);

            // Act
            var exception = await Record.ExceptionAsync(() => target.Handle(command, CancellationToken.None));

            // Assert
            Assert.Null(exception);
            Assert.Null(actor.Credentials);
        }

        [Fact]
        public async Task Handle_ActorHasOtherCredentialsThanCertificate_ThrowsInvalidoperationException()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var certificateServiceMock = new Mock<ICertificateService>();
            var target = new RemoveActorCertificateHandler(actorRepositoryMock.Object, certificateServiceMock.Object);

            var actorId = Guid.NewGuid();
            var actor = TestPreparationModels.MockedActor(actorId);
            actor.Credentials = new ActorClientSecretCredentials("mocked");

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var command = new RemoveActorCertificateCommand(actorId);

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => target.Handle(command, CancellationToken.None));
            Assert.NotNull(actor.Credentials);
        }
    }
}
