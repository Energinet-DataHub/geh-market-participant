﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contacts;
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
public sealed class GetActorContactHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
{
    [Fact]
    public async Task GetActorContact_ValidCommand_HasResult()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actor = await fixture.PrepareActiveActorAsync();

        var createActorContactDto = new CreateActorContactDto(
            "test",
            ContactCategory.Default,
            new RandomlyGeneratedEmailAddress().ToString(),
            "12345678");

        var createCommand = new CreateActorContactCommand(actor.Id, createActorContactDto);
        var fetchCommand = new GetActorContactsCommand(actor.Id);

        // Act + Assert
        var response = await mediator.Send(createCommand);
        var contactGetResponse = await mediator.Send(fetchCommand);
        Assert.NotNull(response);
        Assert.NotNull(contactGetResponse);
        Assert.NotEqual(Guid.Empty, response.ContactId);
        Assert.NotEmpty(contactGetResponse.Contacts);
        Assert.Single(contactGetResponse.Contacts);
        Assert.Equal(createActorContactDto.Name, contactGetResponse.Contacts.First().Name);
        Assert.Equal(createActorContactDto.Category, contactGetResponse.Contacts.First().Category);
        Assert.Equal(createActorContactDto.Email, contactGetResponse.Contacts.First().Email);
        Assert.Equal(createActorContactDto.Phone, contactGetResponse.Contacts.First().Phone);
    }
}
