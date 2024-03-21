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
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetPermissionHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task GetPermission_ValidCommand_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var fetchCommand = new GetPermissionCommand((int)PermissionId.ActorsManage);

        // Act + Assert
        await mediator.Send(fetchCommand);
        var response = await mediator.Send(fetchCommand);
        Assert.NotNull(response);
        Assert.NotNull(response.Permission);
        Assert.Equal((int)PermissionId.ActorsManage, response.Permission.Id);
        Assert.NotEmpty(response.Permission.AssignableTo);
    }
}
