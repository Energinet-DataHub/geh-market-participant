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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetUserRoleIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetUserRoleIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserRole_Found_ReturnsUserRole()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var userRoleId = await _fixture.DatabaseManager.CreateRoleTemplateAsync();

        var mediator = scope.GetInstance<IMediator>();

        var command = new GetUserRoleCommand(userRoleId.Value);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.NotEmpty(response.Role.Name);
        Assert.Equal(userRoleId.Value, response.Role.Id);
    }

    [Fact]
    public async Task GetUserRole_NotFound_ThrowsNotFoundValidationException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var mediator = scope.GetInstance<IMediator>();
        var command = new GetUserRoleCommand(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => mediator.Send(command));
    }
}
