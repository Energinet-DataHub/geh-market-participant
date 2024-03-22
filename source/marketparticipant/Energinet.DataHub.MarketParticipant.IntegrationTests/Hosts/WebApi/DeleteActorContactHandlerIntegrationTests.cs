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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
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
public sealed class DeleteActorContactHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task DeleteActorContact_ValidCommand_IsRemoved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actor = await fixture.PrepareActiveActorAsync();

        var createActorContactDto = new CreateActorContactDto(
            "test",
            ContactCategory.Default,
            new MockedEmailAddress().ToString(),
            "12345678");

        var createCommand = new CreateActorContactCommand(actor.Id, createActorContactDto);
        var fetchCommand = new GetActorContactsCommand(actor.Id);
        var createResponse = await mediator.Send(createCommand);
        var contactGetResponse = await mediator.Send(fetchCommand);

        // Act + Assert
        var deleteCommand = new DeleteActorContactCommand(actor.Id, createResponse.ContactId);
        await mediator.Send(deleteCommand);
        var contactAfterDeleteGetResponse = await mediator.Send(fetchCommand);
        Assert.NotNull(contactGetResponse.Contacts);
        Assert.NotEmpty(contactGetResponse.Contacts);
        Assert.NotNull(contactAfterDeleteGetResponse);
        Assert.Empty(contactAfterDeleteGetResponse.Contacts);
    }
}
