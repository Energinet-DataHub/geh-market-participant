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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateUserRoleHandlerTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public CreateUserRoleHandlerTests(MarketParticipantDatabaseFixture databaseFixture)
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
}
