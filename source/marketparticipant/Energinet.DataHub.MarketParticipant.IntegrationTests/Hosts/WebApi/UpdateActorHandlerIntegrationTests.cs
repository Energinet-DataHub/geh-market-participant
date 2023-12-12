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

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UpdateActorHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateActorHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateActorName_ActiveActorNewName_Success()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();

        var actorEntity = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(e => e.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);

        var newName = "ActorNameUpdated";

        var actor = await actorRepository.GetAsync(new ActorId(actorEntity.Id));
        var actorDto = OrganizationMapper.Map(actor!);

        var updateCommand = new UpdateActorCommand(
            actorEntity.Id,
            new ChangeActorDto(ActorStatus.Active.ToString(), new ActorNameDto(newName), actorDto.MarketRoles));

        // Act
        await mediator.Send(updateCommand);

        var getActorCommand = new GetSingleActorCommand(actorEntity.Id);
        var response = await mediator.Send(getActorCommand);

        // Assert
        Assert.Equal(newName, response.Actor.Name.Value);
    }

    [Fact]
    public async Task UpdateActorMarketRoles_NewActorState_Success()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();

        var marketRoleToAdd = TestPreparationEntities.ValidMarketRole.Patch(m => m.Function = EicFunction.SystemOperator);

        var actorEntity = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole,
            marketRoleToAdd);

        var actor = await actorRepository.GetAsync(new ActorId(actorEntity.Id));
        actor!.RemoveMarketRole(actor.MarketRoles.Single(m => m.Function == EicFunction.SystemOperator));

        var actorDto = OrganizationMapper.Map(actor!);

        var updateCommand = new UpdateActorCommand(
            actorEntity.Id,
            new ChangeActorDto(ActorStatus.New.ToString(), new ActorNameDto(actor.Name.Value), actorDto.MarketRoles));

        // Act
        await mediator.Send(updateCommand);

        var getActorCommand = new GetSingleActorCommand(actorEntity.Id);
        var response = await mediator.Send(getActorCommand);

        // Assert
        Assert.Single(response.Actor.MarketRoles);
        Assert.Equal(actor.Name.Value, response.Actor.Name.Value);
        Assert.Equal(actor.Status.ToString(), response.Actor.Status);
    }

    [Fact]
    public async Task UpdateActorMarketRoles_ActiveActorState_Exception()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();

        var marketRoleToAdd = TestPreparationEntities.ValidMarketRole.Patch(m => m.Function = EicFunction.SystemOperator);

        var actorEntity = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole,
            marketRoleToAdd);

        var actor = await actorRepository.GetAsync(new ActorId(actorEntity.Id));
        Assert.Throws<ValidationException>(() => actor!.RemoveMarketRole(actor.MarketRoles.Single(m => m.Function == EicFunction.SystemOperator)));
    }
}
