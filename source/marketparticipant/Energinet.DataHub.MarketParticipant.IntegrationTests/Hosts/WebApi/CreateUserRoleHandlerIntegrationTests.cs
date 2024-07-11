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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateUserRoleHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public CreateUserRoleHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task CreateUserRole_WhenCalled_CanReadBack()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        // act
        var id = await host.InScopeAsync(
            async sp => (await sp.GetRequiredService<IMediator>().Send(new CreateUserRoleCommand(new CreateUserRoleDto("Name", "desc", UserRoleStatus.Active, EicFunction.Delegated, [])))).UserRoleId);

        var actual = await host.InScopeAsync(
            async sp => await sp.GetRequiredService<IUserRoleRepository>().GetAsync(new UserRoleId(id)));

        // assert
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task CreateUserRole_NonUniqueName_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userRoleCommand = new CreateUserRoleCommand(new CreateUserRoleDto(
            Guid.NewGuid().ToString(),
            "desc",
            UserRoleStatus.Active,
            EicFunction.DataHubAdministrator,
            []));

        await mediator.Send(userRoleCommand);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(userRoleCommand));
    }

    [Fact]
    public async Task CreateUserRole_NonUniqueNameButInactive_Allowed()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userRoleDto = new CreateUserRoleDto(
            Guid.NewGuid().ToString(),
            "desc",
            UserRoleStatus.Inactive,
            EicFunction.DataHubAdministrator,
            []);

        await mediator.Send(new CreateUserRoleCommand(userRoleDto));

        // Act
        var response = await mediator.Send(new CreateUserRoleCommand(userRoleDto with { Status = UserRoleStatus.Active }));

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task CreateUserRole_DisallowedPermission_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userRoleCommand = new CreateUserRoleCommand(new CreateUserRoleDto(
            Guid.NewGuid().ToString(),
            "desc",
            UserRoleStatus.Active,
            EicFunction.Delegated,
            KnownPermissions.All.Where(p => !p.AssignableTo.Contains(EicFunction.Delegated)).Select(p => (int)p.Id)));

        await mediator.Send(userRoleCommand);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(userRoleCommand));
    }
}
