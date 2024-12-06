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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class ActorCredentialsRemovalServiceTests
{
    [Fact]
    public async Task Handle_ActorHasNoCertificateCredentials_ReturnsOk()
    {
        // Arrange
        var actorRepositoryMock = new Mock<ICertificateService>();
        var domainEventRepositoryMock = new Mock<IActorClientSecretService>();
        var target = new ActorCredentialsRemovalService(
            actorRepositoryMock.Object,
            domainEventRepositoryMock.Object);

        var actorId = Guid.NewGuid();
        var actor = TestPreparationModels.MockedActor(actorId);

        // Act
        await target.RemoveActorCredentialsAsync(actor);

        // Assert
        Assert.Null(actor.Credentials);
        actorRepositoryMock.Verify(mock => mock.RemoveCertificateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ActorHasValidCertificateCredentials_ReturnsOk()
    {
        // Arrange
        var actorRepositoryMock = new Mock<ICertificateService>();
        var domainEventRepositoryMock = new Mock<IActorClientSecretService>();
        var target = new ActorCredentialsRemovalService(
            actorRepositoryMock.Object,
            domainEventRepositoryMock.Object);

        var actorId = Guid.NewGuid();
        var actor = TestPreparationModels.MockedActor(actorId);
        actor.Credentials = new ActorCertificateCredentials(
            "mocked",
            "mocked",
            DateTime.UtcNow.AddYears(1).ToInstant());

        // Act
        await target.RemoveActorCredentialsAsync(actor);

        // Assert
        Assert.Null(actor.Credentials);
    }
}
