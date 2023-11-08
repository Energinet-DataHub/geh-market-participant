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
using Azure;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class RemoveActorCertificateHandlerIntegrationTests : IClassFixture<KeyCertificateFixture>
{
    private const string IntegrationActorTestCertificatePublicCer = "integration-actor-test-certificate-public.cer";
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly KeyCertificateFixture _keyCertificateFixture;

    public RemoveActorCertificateHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture,
        KeyCertificateFixture keyCertificateFixture)
    {
        _databaseFixture = databaseFixture;
        _keyCertificateFixture = keyCertificateFixture;
    }

    [Fact]
    public async Task RemoveCertificate_Ok()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        var actor = await _databaseFixture.PrepareActorAsync();

        var certInfo = await _keyCertificateFixture.GetPublicKeyTestCertificateAsync(IntegrationActorTestCertificatePublicCer);
        await _databaseFixture.AssignActorCredentialsAsync(actor.Id, certInfo.Thumbprint, certInfo.CertificateName);

        SetUpCertificateServiceWithFixtureClient(host);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveActorCredentialsCommand(actor.Id);

        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorBeforeUpdate = await actorRepository.GetAsync(new ActorId(actor.Id));

        // Act
        await mediator.Send(command);

        // Assert
        var updatedActor = await actorRepository.GetAsync(new ActorId(actor.Id));
        Assert.NotNull(actorBeforeUpdate);
        Assert.NotNull(updatedActor);
        Assert.True(actorBeforeUpdate.Credentials is ActorCertificateCredentials);
        Assert.Null(updatedActor.Credentials);
        var requestException = await Assert.ThrowsAsync<RequestFailedException>(() => _keyCertificateFixture.CertificateClient.GetSecretAsync(certInfo.CertificateName));
        Assert.Equal(404, requestException.Status);

        // clean up
        await _databaseFixture.AssignActorCredentialsAsync(actor.Id, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
    }

    private void SetUpCertificateServiceWithFixtureClient(WebApiIntegrationTestHost host)
    {
        var certificateService = new CertificateService(
            _keyCertificateFixture.CertificateClient,
            new CertificateValidation(),
            new Mock<ILogger<CertificateService>>().Object);

        host.ServiceCollection.Replace(ServiceDescriptor.Singleton<ICertificateService>(certificateService));
    }
}
