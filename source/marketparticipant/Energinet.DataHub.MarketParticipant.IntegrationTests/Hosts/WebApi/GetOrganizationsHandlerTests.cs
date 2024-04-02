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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
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
public sealed class GetOrganizationsHandlerTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetOrganizationsHandlerTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task GetOrganizations_WhenCalledWithNull_ReturnsAllOrganizations()
    {
        // arrange
        List<OrganizationEntity> organizations = [
            await _databaseFixture.PrepareOrganizationAsync(),
            await _databaseFixture.PrepareOrganizationAsync()];

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        var actual = await target.Send(new GetOrganizationsCommand(null));

        // assert
        Assert.Contains(actual.Organizations, x => x.OrganizationId == organizations[0].Id);
        Assert.Contains(actual.Organizations, x => x.OrganizationId == organizations[1].Id);
    }

    [Fact]
    public async Task GetOrganizations_WhenCalledWithSpecificId_ReturnsOnlyThatOrganization()
    {
        // arrange
        List<OrganizationEntity> organizations = [
            await _databaseFixture.PrepareOrganizationAsync(),
            await _databaseFixture.PrepareOrganizationAsync()];

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        var actual = await target.Send(new GetOrganizationsCommand(organizations[0].Id));

        // assert
        Assert.Single(actual.Organizations);
        Assert.Contains(actual.Organizations, x => x.OrganizationId == organizations[0].Id);
    }
}
