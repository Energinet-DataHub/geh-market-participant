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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetAvailableUserRolesForActorIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetAvailableUserRolesForActorIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAvailableUserRolesForActor_NoUserRoles_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.MarketRoles.Clear()),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.SystemOperator));

        var command = new GetAvailableUserRolesForActorCommand(actor.Id);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Empty(response.Roles);
    }

    [Fact]
    public async Task GetAvailableUserRolesForActor_HasTwoUserRoles_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BalanceResponsibleParty));

        var userRole1 = await _fixture.PrepareUserRoleAsync(EicFunction.BalanceResponsibleParty);
        var userRole2 = await _fixture.PrepareUserRoleAsync(EicFunction.BillingAgent);

        var command = new GetAvailableUserRolesForActorCommand(actor.Id);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Contains(response.Roles, t => t.Id == userRole1.Id);
        Assert.Contains(response.Roles, t => t.Id == userRole2.Id);
    }

    [Fact]
    public async Task GetAvailableUserRolesForActor_HasTwoFunctions_DoesNotReturn()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BalanceResponsibleParty),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var userRole = await _fixture.PrepareUserRoleAsync(
            EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent,
            EicFunction.GridAccessProvider);

        var command = new GetAvailableUserRolesForActorCommand(actor.Id);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.DoesNotContain(response.Roles, t => t.Id == userRole.Id);
    }
}
