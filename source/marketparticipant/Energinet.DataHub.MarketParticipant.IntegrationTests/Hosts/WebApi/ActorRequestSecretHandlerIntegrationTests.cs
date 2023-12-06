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
public sealed class ActorRequestSecretHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly B2CFixture _b2CFixture;

    public ActorRequestSecretHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture, B2CFixture b2CFixture)
    {
        _databaseFixture = databaseFixture;
        _b2CFixture = b2CFixture;
    }

    [Fact]
    public async Task RequestSecret_ActorHasNoCredentials_CreatesNewSecretInB2CAndUpdatesActor()
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

            var actorEntity = await _databaseFixture.PrepareActorAsync(
                TestPreparationEntities.ValidOrganization,
                TestPreparationEntities.ValidActor.Patch(x =>
                {
                    x.ActorNumber = gln;
                    x.ActorId = actor.ExternalActorId!.Value;
                }),
                TestPreparationEntities.ValidMarketRole);

            await using var scope = host.BeginScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new ActorRequestSecretCommand(actorEntity.Id);

            // act
            await mediator.Send(command);

            // asert
            var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
            var actual = (await actorRepository.GetAsync(new ActorId(actorEntity.Id)))?.Credentials as ActorClientSecretCredentials;

            Assert.NotNull(actual);
        }
        finally
        {
            // cleanup
            await _b2CFixture.B2CService.DeleteAppRegistrationAsync(actor);
        }
    }

    [Fact]
    public async Task RequestSecret_ActorHasCredentials_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture, _b2CFixture);

        var externalActorId = Guid.NewGuid();

        var actor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(x =>
            {
                x.ActorId = externalActorId;
                x.ClientSecretCredential = new ActorClientSecretCredentialsEntity
                {
                    ClientSecretIdentifier = Guid.NewGuid().ToString(),
                    ExpirationDate = DateTimeOffset.Now,
                };
            }),
            TestPreparationEntities.ValidMarketRole);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ActorRequestSecretCommand(actor.Id);

        // act, assert
        var actual = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));

        Assert.Equal("Credentials have already been assigned.", actual.Message);
    }

    [Fact]
    public async Task RequestSecret_ActorHasNoExternalId_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture, _b2CFixture);

        var actor = await _databaseFixture.PrepareActorAsync();

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ActorRequestSecretCommand(actor.Id);

        // act, assert
        var actual = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));

        Assert.Equal("Can't request a new secret, as the actor is either not Active or is still being created.", actual.Message);
    }
}
