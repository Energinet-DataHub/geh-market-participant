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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UpdateGridAreaHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateGridAreaHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateGridArea_NewGridAreaName_ChangePersisted()
    {
        // Create context user
        var frontendUser = await _fixture.PrepareUserAsync();

        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var gridArea = await _fixture.PrepareGridAreaAsync();
        var newName = "NewGridAreaName";

        var updateCommand = new UpdateGridAreaCommand(gridArea.Id, new ChangeGridAreaDto(gridArea.Id, newName));

        // Act
        await mediator.Send(updateCommand);

        // Assert
        var response = await mediator.Send(new GetGridAreaCommand(gridArea.Id));
        Assert.Equal(newName, response.GridArea.Name);
    }

    [Fact]
    public async Task UpdateGridArea_NewGridAreaName_ChangeAudited()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        host.ServiceCollection.MockFrontendUser(KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId.Value);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var gridArea = await _fixture.PrepareGridAreaAsync();

        // Act
        var updateCommand1 = new UpdateGridAreaCommand(gridArea.Id, new ChangeGridAreaDto(gridArea.Id, "NewGridAreaName1"));
        await mediator.Send(updateCommand1);

        var updateCommand2 = new UpdateGridAreaCommand(gridArea.Id, new ChangeGridAreaDto(gridArea.Id, "NewGridAreaName2"));
        await mediator.Send(updateCommand2);

        var updateCommand3 = new UpdateGridAreaCommand(gridArea.Id, new ChangeGridAreaDto(gridArea.Id, "NewGridAreaName3"));
        await mediator.Send(updateCommand3);

        // Assert
        var response = await mediator.Send(new GetGridAreaAuditLogEntriesCommand(gridArea.Id));
        var auditLogs = response.GridAreaAuditLogEntries.ToList();

        Assert.Equal("NewGridAreaName1", auditLogs[0].NewValue);
        Assert.Equal("NewGridAreaName2", auditLogs[1].NewValue);
        Assert.Equal("NewGridAreaName3", auditLogs[2].NewValue);
        Assert.Equal(KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId.Value, auditLogs[0].AuditIdentityId);
        Assert.Equal(KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId.Value, auditLogs[1].AuditIdentityId);
        Assert.Equal(KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId.Value, auditLogs[2].AuditIdentityId);
    }
}
