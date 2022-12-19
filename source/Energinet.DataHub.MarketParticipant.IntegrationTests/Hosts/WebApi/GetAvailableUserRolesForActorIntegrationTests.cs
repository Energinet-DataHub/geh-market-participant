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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetAvailableUserRolesForActorIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetAvailableUserRolesForActorIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserRoleTemplatesForActor_NoTemplates_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var actorId = await _fixture
            .DatabaseManager
            .CreateActorAsync(new[] { EicFunction.CoordinatedCapacityCalculator });

        var command = new GetAvailableUserRolesForActorCommand(actorId);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Empty(response.Roles);
    }

    [Fact]
    public async Task GetUserRoleTemplatesForActor_HasTwoTemplates_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var mediator = scope.GetInstance<IMediator>();

        var actorId = await _fixture
            .DatabaseManager
            .CreateActorAsync(new[] { EicFunction.Agent, EicFunction.CapacityTrader });

        var userRoleTemplate1 = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            EicFunctions =
            {
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent }
            }
        };

        var userRoleTemplate2 = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            EicFunctions =
            {
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.CapacityTrader }
            }
        };

        await context.UserRoles.AddAsync(userRoleTemplate1);
        await context.UserRoles.AddAsync(userRoleTemplate2);
        await context.SaveChangesAsync();

        var command = new GetAvailableUserRolesForActorCommand(actorId);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Contains(response.Roles, t => t.Id == userRoleTemplate1.Id);
        Assert.Contains(response.Roles, t => t.Id == userRoleTemplate2.Id);
    }

    [Fact]
    public async Task GetUserRoleTemplatesForActor_HasTwoFunctions_DoesNotReturn()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var mediator = scope.GetInstance<IMediator>();

        var actorId = await _fixture
            .DatabaseManager
            .CreateActorAsync(new[] { EicFunction.Agent, EicFunction.CapacityTrader });

        var userRoleTemplate = new UserRoleEntity
        {
            Id = Guid.NewGuid(),
            EicFunctions =
            {
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.Agent },
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.CapacityTrader },
                new UserRoleEicFunctionEntity { EicFunction = EicFunction.Consumer },
            }
        };

        await context.UserRoles.AddAsync(userRoleTemplate);
        await context.SaveChangesAsync();

        var command = new GetAvailableUserRolesForActorCommand(actorId);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.DoesNotContain(response.Roles, t => t.Id == userRoleTemplate.Id);
    }
}
