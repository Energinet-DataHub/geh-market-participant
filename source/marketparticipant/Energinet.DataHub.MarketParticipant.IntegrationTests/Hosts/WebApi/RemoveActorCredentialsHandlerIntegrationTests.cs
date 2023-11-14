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
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class RemoveActorCredentialsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly B2CFixture _b2CFixture;
    private readonly CertificateFixture _certificateFixture;

    public RemoveActorCredentialsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture,
        B2CFixture b2CFixture,
        CertificateFixture certificateFixture)
    {
        _databaseFixture = databaseFixture;
        _b2CFixture = b2CFixture;
        _certificateFixture = certificateFixture;
    }

    [Fact]
    public async Task RemoveActorSecret_ActorSecretExists_IsRemoved()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture, _b2CFixture);

        CreateAppRegistrationResponse? appRegistration = null;
        try
        {
            var gln = new MockedGln();

            appRegistration = await _b2CFixture.B2CService.CreateAppRegistrationAsync(gln, new[]
            {
                EicFunction.EnergySupplier,
            });

            var secret = await _b2CFixture.B2CService.CreateSecretForAppRegistrationAsync(appRegistration.ExternalActorId);

            var actor = await _databaseFixture.PrepareActorAsync(
                TestPreparationEntities.ValidOrganization,
                TestPreparationEntities.ValidActor.Patch(x =>
                {
                    x.ActorNumber = gln;
                    x.ActorId = appRegistration.ExternalActorId.Value;
                    x.ClientSecretCredential = new ActorClientSecretCredentialsEntity
                    {
                        ExpirationDate = secret.ExpirationDate,
                        ClientSecretIdentifier = secret.SecretId.ToString(),
                    };
                }),
                TestPreparationEntities.ValidMarketRole);

            Assert.NotNull((await GetActor(host, actor))?.Credentials as ActorClientSecretCredentials);

            // act
            await SendCommand(host, new RemoveActorCredentialsCommand(actor.Id));

            // asert
            Assert.Null((await GetActor(host, actor))?.Credentials as ActorClientSecretCredentials);
        }
        finally
        {
            // cleanup
            if (appRegistration != null)
            {
                await _b2CFixture.B2CService.DeleteAppRegistrationAsync(appRegistration.ExternalActorId);
            }
        }
    }

    [Fact]
    public async Task RemoveActorSecret_NoExternalIdOnActor_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var gln = new MockedGln();

        var actor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(x =>
            {
                x.ActorNumber = gln;
                x.ClientSecretCredential = new ActorClientSecretCredentialsEntity
                {
                    ExpirationDate = DateTimeOffset.Now,
                    ClientSecretIdentifier = Guid.NewGuid().ToString(),
                };
            }),
            TestPreparationEntities.ValidMarketRole);

        // act
        var actual = await Assert.ThrowsAsync<ValidationException>(() => SendCommand(host, new RemoveActorCredentialsCommand(actor.Id)));

        // asert
        Assert.Equal("Can't remove secret, as the actor is either not Active or is still being created", actual.Message);
        Assert.NotNull((await GetActor(host, actor))?.Credentials as ActorClientSecretCredentials);
    }

    [Fact]
    public async Task RemoveActorCertificate_ActorCertificateExists_IsRemoved()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture, certificateFixture: _certificateFixture);

        var certificateName = Guid.NewGuid().ToString();
        try
        {
            var certificate = await _certificateFixture.CreatePublicKeyCertificateAsync(certificateName);
            var gln = new MockedGln();

            var actor = await _databaseFixture.PrepareActorAsync(
                TestPreparationEntities.ValidOrganization,
                TestPreparationEntities.ValidActor.Patch(x =>
                {
                    x.ActorNumber = gln;
                    x.CertificateCredential = new ActorCertificateCredentialsEntity
                    {
                        CertificateThumbprint = certificate.Thumbprint,
                        KeyVaultSecretIdentifier = certificate.CertificateName,
                        ExpirationDate = certificate.ExpirationDate,
                    };
                }),
                TestPreparationEntities.ValidMarketRole);

            Assert.NotNull((await GetActor(host, actor))?.Credentials as ActorCertificateCredentials);
            Assert.True(await _certificateFixture.CertificateExistsAsync(certificateName));

            // act
            await SendCommand(host, new RemoveActorCredentialsCommand(actor.Id));

            // asert
            Assert.Null((await GetActor(host, actor))?.Credentials as ActorCertificateCredentials);
            Assert.False(await _certificateFixture.CertificateExistsAsync(certificateName));
        }
        finally
        {
            // cleanup
            await _certificateFixture.CleanUpCertificateFromStorageAsync(certificateName);
        }
    }

    [Fact]
    public async Task RemoveActorCertificate_ActorNotFound_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        // act, assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => SendCommand(host, new RemoveActorCredentialsCommand(Guid.NewGuid())));
    }

    private static async Task<Actor?> GetActor(WebApiIntegrationTestHost host, ActorEntity actor)
    {
        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        return await actorRepository.GetAsync(new ActorId(actor.Id));
    }

    private static async Task SendCommand(WebApiIntegrationTestHost host, IRequest actor)
    {
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(actor);
    }
}
