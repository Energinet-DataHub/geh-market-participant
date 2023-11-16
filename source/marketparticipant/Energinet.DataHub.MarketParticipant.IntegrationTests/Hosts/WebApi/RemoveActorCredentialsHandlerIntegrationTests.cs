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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
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
    private readonly SecretFixture _secretFixture;

    public RemoveActorCredentialsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture,
        B2CFixture b2CFixture,
        CertificateFixture certificateFixture,
        SecretFixture secretFixture)
    {
        _databaseFixture = databaseFixture;
        _b2CFixture = b2CFixture;
        _certificateFixture = certificateFixture;
        _secretFixture = secretFixture;
    }

    [Fact]
    public async Task RemoveActorSecret_ActorSecretExists_IsRemoved()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture, _b2CFixture);

        var gln = new MockedGln();

        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            new[]
            {
                new ActorMarketRole(EicFunction.EnergySupplier)
            },
            new ActorName(Guid.NewGuid().ToString()),
            null);

        try
        {
            await _b2CFixture.B2CService.AssignApplicationRegistrationAsync(actor);

            actor.ExternalActorId = actor.ExternalActorId;

            var secret = await _secretFixture.ClientSecretService.CreateSecretAsync(actor);

            var actorEntity = await _databaseFixture.PrepareActorAsync(
                TestPreparationEntities.ValidOrganization,
                TestPreparationEntities.ValidActor.Patch(x =>
                {
                    x.ActorNumber = gln;
                    x.ActorId = actor.ExternalActorId!.Value;
                    x.ClientSecretCredential = new ActorClientSecretCredentialsEntity
                    {
                        ExpirationDate = secret.ExpirationDate.ToDateTimeOffset(),
                        ClientSecretIdentifier = secret.SecretId.ToString(),
                    };
                }),
                TestPreparationEntities.ValidMarketRole);

            Assert.NotNull((await GetActor(host, actorEntity))?.Credentials as ActorClientSecretCredentials);

            // act
            await SendCommand(host, new RemoveActorCredentialsCommand(actorEntity.Id));

            // asert
            Assert.Null((await GetActor(host, actorEntity))?.Credentials as ActorClientSecretCredentials);
        }
        finally
        {
            // cleanup
            await _b2CFixture.B2CService.DeleteAppRegistrationAsync(actor);
        }
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
                        KeyVaultSecretIdentifier = certificateName,
                        ExpirationDate = certificate.NotAfter,
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
