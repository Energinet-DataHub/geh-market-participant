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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateActorHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public CreateActorHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateActor_MarketRole_Delegated_Success()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actorName = new ActorNameDto("Actor Delegated");
        var newOrganization = await _fixture.PrepareOrganizationAsync();
        var actorNumber = new ActorNumberDto(new MockedGln());
        var marketRoles = ActorMarketRoleDto(EicFunction.Delegated);

        var createDto = new CreateActorDto(newOrganization.Id, actorName, actorNumber, marketRoles);

        var createActorCommand = new CreateActorCommand(createDto);

        // Act
        var createResponse = await mediator.Send(createActorCommand);

        var getActorCommand = new GetSingleActorCommand(createResponse.ActorId);
        var actualActor = await mediator.Send(getActorCommand);

        // Assert
        Assert.Equal(createResponse.ActorId, actualActor.Actor.ActorId);
        Assert.Equal(createDto.Name.Value, actualActor.Actor.Name.Value);
        actualActor.Actor.MarketRoles.Should().ContainSingle(x => x.EicFunction == EicFunction.Delegated);
    }

    [Fact]
    public async Task CreateMultiActors_MarketRole_Delegated_SameActorNumber_Success()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var newOrganization = await _fixture.PrepareOrganizationAsync();
        var actorNumber = new ActorNumberDto(new MockedGln());

        var createInitActorDto = new CreateActorDto(newOrganization.Id, new ActorNameDto("Actor Init"), actorNumber, ActorMarketRoleDto(EicFunction.SystemOperator));

        var createInitActorCommand = new CreateActorCommand(createInitActorDto);
        var initActorResponse = await mediator.Send(createInitActorCommand);

        // Act
        var createDelegatedActorDto = new CreateActorDto(newOrganization.Id, new ActorNameDto("Actor Delegated"), actorNumber, ActorMarketRoleDto(EicFunction.Delegated));
        var createDelegatedActorCommand = new CreateActorCommand(createDelegatedActorDto);
        var createResponseDelegatedActor = await mediator.Send(createDelegatedActorCommand);

        var getDelegatedActorCommand = new GetSingleActorCommand(createResponseDelegatedActor.ActorId);
        var actualDelegatedActor = await mediator.Send(getDelegatedActorCommand);

        var getInitActorCommand = new GetSingleActorCommand(initActorResponse.ActorId);
        var actualInitActor = await mediator.Send(getInitActorCommand);

        // Assert
        Assert.Equal(createResponseDelegatedActor.ActorId, actualDelegatedActor.Actor.ActorId);
        Assert.Equal(actualInitActor.Actor.ActorNumber, actualDelegatedActor.Actor.ActorNumber);
        actualDelegatedActor.Actor.MarketRoles.Should().ContainSingle(x => x.EicFunction == EicFunction.Delegated);
    }

    [Fact]
    public async Task CreateActor_NonUniqueGlnAndRole_ThrowsException()
    {
        // Arrange
        var actorName = new ActorNameDto("Actor Delegated");
        var actorNumber = new ActorNumberDto(new MockedGln());
        var marketRoles = ActorMarketRoleDto(EicFunction.Delegated);

        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
        {
            var cvr = MockedBusinessRegisterIdentifier.New().Identifier;

            tasks.Add(Task.Run(async () =>
            {
                await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
                await using var scope = host.BeginScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var newOrganization = await _fixture.PrepareOrganizationAsync(TestPreparationEntities.ValidOrganization.Patch(o => o.BusinessRegisterIdentifier = cvr));

                var createDto = new CreateActorDto(newOrganization.Id, actorName, actorNumber, marketRoles);
                var createActorCommand = new CreateActorCommand(createDto);

                return await mediator.Send(createActorCommand);
            }));
        }

        // Act + Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => tasks.WhenAll());
        Assert.Equal("actor.number.reserved", exception.Data[ValidationExceptionExtensions.ErrorCodeDataKey]);
    }

    private static IEnumerable<ActorMarketRoleDto> ActorMarketRoleDto(EicFunction eicFunction)
    {
        return new ActorMarketRoleDto[]
        {
            new(eicFunction, Array.Empty<ActorGridAreaDto>(), null)
        };
    }
}
