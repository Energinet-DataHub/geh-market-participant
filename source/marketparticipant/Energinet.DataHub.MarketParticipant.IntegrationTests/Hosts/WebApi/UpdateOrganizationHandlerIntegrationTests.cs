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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
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
public sealed class UpdateOrganizationHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateOrganizationHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateOrganization_InvalidCommand_CanReadBack()
    {
        // Arrange
        const string blankValue = "  ";

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var organizationDto = new CreateOrganizationDto(
            "Test Organization",
            MockedBusinessRegisterIdentifier.New().Identifier,
            new AddressDto(null, null, null, null, "DK"),
            new MockedDomain());
        var updatedDomain = new MockedDomain();
        var updateDto = new ChangeOrganizationDto("New Name", OrganizationStatus.Active.ToString(), updatedDomain);
        var command = new CreateOrganizationCommand(organizationDto);
        var orgResponse = await mediator.Send(command);
        var updateCommand = new UpdateOrganizationCommand(orgResponse.OrganizationId, updateDto);
        var getCommand = new GetSingleOrganizationCommand(orgResponse.OrganizationId);

        // Act + Assert
        await mediator.Send(updateCommand);
        var updatedOrg = await mediator.Send(getCommand);
        Assert.NotNull(updatedOrg);
        Assert.Equal(orgResponse.OrganizationId, updatedOrg.Organization.OrganizationId);
        Assert.Equal(updatedDomain, updatedOrg.Organization.Domain);
        Assert.Equal("New Name", updatedOrg.Organization.Name);
        Assert.Equal(OrganizationStatus.Active.ToString(), updatedOrg.Organization.Status);
    }
}
