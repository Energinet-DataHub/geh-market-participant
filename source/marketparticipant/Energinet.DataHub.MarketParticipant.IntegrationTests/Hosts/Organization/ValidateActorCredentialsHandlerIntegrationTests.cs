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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.Organization;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ValidateActorCredentialsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ValidateActorCredentialsHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_ClientSecretExpireSoon_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var credentialsCloseToExpiring = new ActorClientSecretCredentialsEntity
        {
            ClientSecretIdentifier = Guid.NewGuid().ToString(),
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(29)
        };

        var targetActor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a =>
            {
                a.Status = ActorStatus.Active;
                a.ActorId = Guid.NewGuid();
                a.ClientSecretCredential = credentialsCloseToExpiring;
            }),
            TestPreparationEntities.ValidMarketRole);

        // Act
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateActorCredentialsCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
            It.Is<ActorCredentialsExpiring>(notification =>
                notification.AffectedActorId.Value == targetActor.Id &&
                notification.Recipient.Value == targetActor.Id)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CertificateExpireSoon_SendsNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var credentialsCloseToExpiring = new ActorCertificateCredentialsEntity
        {
            KeyVaultSecretIdentifier = Guid.NewGuid().ToString(),
            CertificateThumbprint = Guid.NewGuid().ToString(),
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(29)
        };

        var targetActor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a =>
            {
                a.Status = ActorStatus.Active;
                a.ActorId = Guid.NewGuid();
                a.CertificateCredential = credentialsCloseToExpiring;
            }),
            TestPreparationEntities.ValidMarketRole);

        // Act
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateActorCredentialsCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<ActorCredentialsExpiring>(notification =>
                    notification.AffectedActorId.Value == targetActor.Id &&
                    notification.Recipient.Value == targetActor.Id)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActorInactive_DoesNotSendNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var credentialsCloseToExpiring = new ActorCertificateCredentialsEntity
        {
            KeyVaultSecretIdentifier = Guid.NewGuid().ToString(),
            CertificateThumbprint = Guid.NewGuid().ToString(),
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(29)
        };

        var targetActor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a =>
            {
                a.Status = ActorStatus.Inactive;
                a.ActorId = Guid.NewGuid();
                a.CertificateCredential = credentialsCloseToExpiring;
            }),
            TestPreparationEntities.ValidMarketRole);

        // Act
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateActorCredentialsCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<ActorCredentialsExpiring>(notification =>
                    notification.AffectedActorId.Value == targetActor.Id &&
                    notification.Recipient.Value == targetActor.Id)),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ActorNoCredentials_DoesNotSendNotification()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        var domainEventRepository = new Mock<IDomainEventRepository>();

        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var targetActor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a =>
            {
                a.Status = ActorStatus.Active;
                a.ActorId = Guid.NewGuid();
            }),
            TestPreparationEntities.ValidMarketRole);

        // Act
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ValidateActorCredentialsCommand());

        // Assert
        domainEventRepository.Verify(
            repo => repo.EnqueueAsync(
                It.Is<ActorCredentialsExpiring>(notification =>
                    notification.AffectedActorId.Value == targetActor.Id &&
                    notification.Recipient.Value == targetActor.Id)),
            Times.Never);
    }
}
