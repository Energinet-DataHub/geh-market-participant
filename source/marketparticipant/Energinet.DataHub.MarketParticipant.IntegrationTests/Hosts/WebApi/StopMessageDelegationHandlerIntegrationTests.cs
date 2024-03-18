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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class StopMessageDelegationHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task StopMessageDelegation_ValidCommand_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actorFrom = await fixture.PrepareActiveActorAsync();
        var actorTo = await fixture.PrepareActiveActorAsync();
        var gridArea = await fixture.PrepareGridAreaAsync();
        var startsAt = DateTimeOffset.UtcNow;

        var messageDelegationDto = new CreateMessageDelegationDto(
            actorFrom.Id,
            actorTo.Id,
            new List<Guid>() { gridArea.Id },
            new List<DelegationMessageType>() { DelegationMessageType.Rsm012Inbound },
            startsAt);

        var createCommand = new CreateMessageDelegationCommand(messageDelegationDto);
        var fetchCommand = new GetDelegationsForActorCommand(actorFrom.Id);
        await mediator.Send(createCommand);
        var response = await mediator.Send(fetchCommand);

        var stopMessageDelegationDto = new StopMessageDelegationDto(response.Delegations.First().Id, response.Delegations.First().Periods.First().Id, DateTimeOffset.UtcNow.AddMonths(5));
        var stopCommand = new StopMessageDelegationCommand(stopMessageDelegationDto);

        // Act + Assert
        await mediator.Send(stopCommand);
        response = await mediator.Send(fetchCommand);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Delegations);
        Assert.Single(response.Delegations);
        Assert.Single(response.Delegations.First().Periods);
        Assert.Contains(response.Delegations, d => d.DelegatedBy == actorFrom.Id);
        Assert.True(response.Delegations.First().Periods.First().DelegatedTo == actorTo.Id);
        Assert.True(response.Delegations.First().Periods.First().GridAreaId == gridArea.Id);
        Assert.True(response.Delegations.First().Periods.First().StartsAt == startsAt);
        Assert.True(response.Delegations.First().Periods.First().ExpiresAt == stopMessageDelegationDto.StopsAt);
    }
}
