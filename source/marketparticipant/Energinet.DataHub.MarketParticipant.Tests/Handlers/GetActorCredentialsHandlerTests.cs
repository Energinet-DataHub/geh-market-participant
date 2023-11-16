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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetActorCredentialsHandlerTests
    {
        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new GetActorCredentialsHandler(actorRepositoryMock.Object);

            var actorId = Guid.NewGuid();

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
                .ReturnsAsync((Actor?)null);

            var command = new GetActorCredentialsCommand(actorId);

            // Act + Assert
            await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_HasNoCredentials_ReturnsNull()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new GetActorCredentialsHandler(actorRepositoryMock.Object);

            var actorId = Guid.NewGuid();
            var actor = TestPreparationModels.MockedActor(actorId);

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var command = new GetActorCredentialsCommand(actorId);

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.Null(response);
        }

        [Fact]
        public async Task Handle_HasCertificateCredentials_ReturnsCorrectCredentials()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new GetActorCredentialsHandler(actorRepositoryMock.Object);

            var actorId = Guid.NewGuid();
            var actor = TestPreparationModels.MockedActor(actorId);
            actor.Credentials = new ActorCertificateCredentials(
                "mock",
                "mock",
                DateTime.UtcNow.AddYears(1).ToInstant());

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var command = new GetActorCredentialsCommand(actorId);

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.CredentialsDto.CertificateCredentials);
            Assert.Null(response.CredentialsDto.ClientSecretCredentials);
        }

        [Fact]
        public async Task Handle_HasClientSecretCredentials_ReturnsCorrectCredentials()
        {
            // Arrange
            var actorRepositoryMock = new Mock<IActorRepository>();
            var target = new GetActorCredentialsHandler(actorRepositoryMock.Object);

            var actorId = Guid.NewGuid();
            var actor = TestPreparationModels.MockedActor(actorId);
            actor.Credentials = new ActorClientSecretCredentials(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow.AddYears(1).ToInstant());

            actorRepositoryMock
                .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
                .ReturnsAsync(actor);

            var command = new GetActorCredentialsCommand(actorId);

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.CredentialsDto.ClientSecretCredentials);
            Assert.Null(response.CredentialsDto.CertificateCredentials);
        }
    }
}
