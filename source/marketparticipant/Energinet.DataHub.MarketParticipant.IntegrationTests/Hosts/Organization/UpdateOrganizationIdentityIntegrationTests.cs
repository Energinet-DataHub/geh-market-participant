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
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.Organization;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UpdateOrganizationIdentityIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateOrganizationIdentityIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateOrganizationIdentity_CompletesWithoutErrors()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);

        await _fixture.EmailEventsClearNotSentAsync();

        var newOrg = await _fixture.PrepareOrganizationAsync();

        var orgIdentityRepository = new Mock<IOrganizationIdentityRepository>();
        orgIdentityRepository
            .Setup(e => e.GetAsync(It.Is<BusinessRegisterIdentifier>(e => e.Identifier == newOrg.BusinessRegisterIdentifier)))
            .ReturnsAsync(new OrganizationIdentity(newOrg.Name + " updated identity"));

        host.ServiceCollection.RemoveAll<IOrganizationIdentityRepository>();
        host.ServiceCollection.AddScoped(_ => orgIdentityRepository.Object);

        await using var scope = host.BeginScope();
        var command = new UpdateOrganisationIdentityTriggerCommand();

        // Act
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(command);

        // Assert
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var emailEventRepository = new EmailEventRepository(context);
        var savedEmailEvents = await emailEventRepository.GetAllPendingEmailEventsAsync();

        var orgResult = await mediator.Send(new GetSingleOrganizationCommand(newOrg.Id));
        orgIdentityRepository.Verify(x => x.GetAsync(It.Is<BusinessRegisterIdentifier>(e => e.Identifier == newOrg.BusinessRegisterIdentifier)), Times.Once);
        Assert.Equal(newOrg.Name + " updated identity", orgResult.Organization.Name);
        Assert.Single(savedEmailEvents, e => e.EmailTemplate.TemplateId is EmailTemplateId.OrganizationIdentityChanged);
    }
}
