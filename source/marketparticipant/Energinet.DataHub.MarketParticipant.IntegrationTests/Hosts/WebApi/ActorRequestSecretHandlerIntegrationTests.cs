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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorRequestSecretHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public ActorRequestSecretHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task RequestSecret_ActorHasNoCredentials_CreatesNewSecretInB2CAndUpdatesActor()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var externalActorId = Guid.NewGuid();
        var secretId = Guid.NewGuid();
        var secret = Guid.NewGuid().ToString();
        var expirationDate = DateTimeOffset.Now;

        var activeDirectoryB2CService = new Mock<IActiveDirectoryB2CService>();
        activeDirectoryB2CService.Setup(x => x.CreateSecretForAppRegistrationAsync(new ExternalActorId(externalActorId)))
            .ReturnsAsync((secretId, secret, expirationDate));

        host.ServiceCollection.Replace(ServiceDescriptor.Scoped<IActiveDirectoryB2CService>(_ => activeDirectoryB2CService.Object));

        var actor = await _databaseFixture.PrepareActorAsync();

        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();
        actor.ActorId = externalActorId;
        context.Update(actor);
        await context.SaveChangesAsync();

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ActorRequestSecretCommand(actor.Id);

        // act
        await mediator.Send(command);

        // asert
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actual = (await actorRepository.GetAsync(new ActorId(actor.Id)))?.Credentials as ActorClientSecretCredentials;

        Assert.NotNull(actual);
        Assert.Equal(secretId, actual.ClientSecretIdentifier);
        Assert.Equal(expirationDate, actual.ExpirationDate);
    }

    [Fact]
    public async Task RequestSecret_ActorHasCredentials_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var externalActorId = Guid.NewGuid();

        host.ServiceCollection.Replace(ServiceDescriptor.Scoped<IActiveDirectoryB2CService>(_ =>
            new Mock<IActiveDirectoryB2CService>().Object));

        var actor = await _databaseFixture.PrepareActorAsync();

        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();
        actor.ActorId = externalActorId;
        actor.ClientSecretCredential = new ActorClientSecretCredentialsEntity
        {
            ClientSecretIdentifier = Guid.NewGuid().ToString(),
            ExpirationDate = DateTimeOffset.Now,
        };
        context.Update(actor);
        await context.SaveChangesAsync();

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ActorRequestSecretCommand(actor.Id);

        // act, assert
        var actual = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));

        Assert.Equal($"Actor with id {actor.Id} Can not have new credentials generated, as it already has credentials", actual.Message);
    }

    [Fact]
    public async Task RequestSecret_ActorHasNoExternalId_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        host.ServiceCollection.Replace(ServiceDescriptor.Scoped<IActiveDirectoryB2CService>(_ =>
            new Mock<IActiveDirectoryB2CService>().Object));

        var actor = await _databaseFixture.PrepareActorAsync();

        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ActorRequestSecretCommand(actor.Id);

        // act, assert
        var actual = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(command));

        Assert.Equal("Can't request a new secret, as the actor is either not Active or is still being created", actual.Message);
    }
}
