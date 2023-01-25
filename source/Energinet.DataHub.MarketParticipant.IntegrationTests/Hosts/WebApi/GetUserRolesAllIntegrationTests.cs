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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetUserRolesAllIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserRolesAllIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserRolesAll_ThreeExists()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var userRoleId1 = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync("Role1", "Description", UserRoleStatus.Active, EicFunction.Consumer, System.Array.Empty<Permission>());

        var userRoleId2 = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync("Role2", "Description", UserRoleStatus.Active, EicFunction.Producer, System.Array.Empty<Permission>());

        var userRoleId3 = await _fixture
            .DatabaseManager
            .CreateUserRoleAsync("Role3", "Description", UserRoleStatus.Active, EicFunction.BillingAgent, System.Array.Empty<Permission>());

        var command1 = new GetAllUserRolesCommand();

        // Act
        var response = await mediator.Send(command1);

        // Assert
        Assert.Contains(response.Roles, r => r.Id == userRoleId1.Value && r.Name == "Role1");
        Assert.Contains(response.Roles, r => r.Id == userRoleId2.Value && r.Name == "Role2");
        Assert.Contains(response.Roles, r => r.Id == userRoleId3.Value && r.Name == "Role3");
    }
}
