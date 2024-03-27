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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
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
public sealed class CreateProcessDelegationHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task CreateProcessDelegation_ValidCommand_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actorFrom = await fixture.PrepareActiveActorAsync();
        var actorTo = await fixture.PrepareActiveActorAsync();
        var gridArea = await fixture.PrepareGridAreaAsync();
        var startsAt = DateTimeOffset.UtcNow;

        var processDelegationDto = new CreateProcessDelegationsDto(
            actorFrom.Id,
            actorTo.Id,
            [gridArea.Id],
            [DelegatedProcess.RequestEnergyResults],
            startsAt);

        var createCommand = new CreateProcessDelegationCommand(processDelegationDto);
        var fetchCommand = new GetDelegationsForActorCommand(actorFrom.Id);

        // Act
        await mediator.Send(createCommand);

        // Assert
        var response = await mediator.Send(fetchCommand);
        Assert.NotNull(response);
        Assert.NotEmpty(response.Delegations);
        Assert.Single(response.Delegations);
        Assert.Single(response.Delegations.First().Periods);
        Assert.Contains(response.Delegations, d => d.DelegatedBy == actorFrom.Id);
        Assert.True(response.Delegations.First().Periods.First().DelegatedTo == actorTo.Id);
        Assert.True(response.Delegations.First().Periods.First().GridAreaId == gridArea.Id);
        Assert.True(response.Delegations.First().Periods.First().StartsAt == startsAt);
    }

    [Fact]
    public async Task CreateProcessDelegation_GridAreaNotAllowed_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();

        var allowedGridArea = await fixture.PrepareGridAreaAsync();
        var inputMarketRoles = new MarketRoleEntity
        {
            Function = EicFunction.GridAccessProvider,
            GridAreas = { new MarketRoleGridAreaEntity { GridAreaId = allowedGridArea.Id } }
        };

        var actorFrom = await fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            inputMarketRoles);

        var actorTo = await fixture.PrepareActiveActorAsync();
        var otherGridArea = await fixture.PrepareGridAreaAsync();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var processDelegationDto = new CreateProcessDelegationsDto(
            actorFrom.Id,
            actorTo.Id,
            [allowedGridArea.Id],
            [DelegatedProcess.RequestEnergyResults],
            DateTimeOffset.UtcNow);

        await mediator.Send(new CreateProcessDelegationCommand(processDelegationDto));

        processDelegationDto = processDelegationDto with { GridAreas = [otherGridArea.Id] };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(new CreateProcessDelegationCommand(processDelegationDto)));
    }
}
