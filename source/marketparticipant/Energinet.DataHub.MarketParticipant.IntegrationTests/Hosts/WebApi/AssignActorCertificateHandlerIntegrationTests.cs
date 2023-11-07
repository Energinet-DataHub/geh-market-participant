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
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Services;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class AssignActorCertificateHandlerIntegrationTests
{
    private const string IntegrationActorTestCertificatePublicCer = "integration-actor-test-certificate-public.cer";
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public AssignActorCertificateHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task AssignCertificate_FlowCompleted()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        var actor = await _databaseFixture.PrepareActorAsync();

        await using var certificateFileStream = SetupTestCertificate(IntegrationActorTestCertificatePublicCer);
        var command = new AssignActorCertificateCommand(actor.Id, certificateFileStream);

        SetUpCertificateServiceWithMockSave(host);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(command);

        // Assert
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var updatedActor = await actorRepository.GetAsync(new ActorId(actor.Id));
        Assert.NotNull(updatedActor?.Credentials);
        Assert.True(updatedActor.Credentials is ActorCertificateCredentials);
    }

    [Fact]
    public async Task AssignCertificate_NoActorFound()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        await using var certificateFileStream = SetupTestCertificate(IntegrationActorTestCertificatePublicCer);
        var command = new AssignActorCertificateCommand(Guid.NewGuid(), certificateFileStream);

        SetUpCertificateServiceWithMockSave(host);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act + assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => mediator.Send(command));
    }

    [Fact]
    public async Task AssignCertificate_CredentialsAlreadySet()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actor = await _databaseFixture.PrepareActorAsync();
        await _databaseFixture.AssignActorCredentialsAsync(actor.Id, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        await using var certificateFileStream = SetupTestCertificate(IntegrationActorTestCertificatePublicCer);
        var command = new AssignActorCertificateCommand(actor.Id, certificateFileStream);

        SetUpCertificateServiceWithMockSave(host);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act + assert
        await Assert.ThrowsAsync<NotSupportedException>(() => mediator.Send(command));
    }

    private static Stream SetupTestCertificate(string certificateName)
    {
        var resourceName = $"Energinet.DataHub.MarketParticipant.IntegrationTests.Common.Certificates.{certificateName}";

        var assembly = typeof(CertificateServiceTests).Assembly;
        var stream = assembly.GetManifestResourceStream(resourceName);

        return stream ?? throw new InvalidOperationException($"Could not find resource {resourceName}");
    }

    private static void SetUpCertificateServiceWithMockSave(WebApiIntegrationTestHost host)
    {
        var certValidation = new Mock<ICertificateValidation>();
        certValidation.Setup(e => e.Verify(It.IsAny<X509Certificate2>()));

        var certificateServiceMock = new Mock<CertificateService>(
                It.IsAny<SecretClient>(), certValidation.Object, It.IsAny<ILogger<CertificateService>>())
            .As<ICertificateService>();
        certificateServiceMock
            .Setup(x => x.SaveCertificateAsync(It.IsAny<string>(), It.IsAny<X509Certificate2>()))
            .Returns(() => Task.CompletedTask);
        certificateServiceMock.CallBase = true;

        host.ServiceCollection.RemoveAll<ICertificateService>();
        host.ServiceCollection.AddSingleton(_ => certificateServiceMock.Object);
    }
}