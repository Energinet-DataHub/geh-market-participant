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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CheckDomainExistsHandlerIntegrationTests(
    MarketParticipantDatabaseFixture databaseFixture)
{
    [Fact]
    public async Task DomainCheck_DomainIsUnknown_ReturnsFalse()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);

        var testEmail = $"test@{new MockedDomain()}";

        var command = new CheckDomainExistsCommand(testEmail);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.False(response);
    }

    [Fact]
    public async Task DomainCheck_DomainIsKnown_ReturnsTrue()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(databaseFixture);
        var organizationEntity = await databaseFixture.PrepareOrganizationAsync();

        var testEmail = $"test@{organizationEntity.Domain}";

        var command = new CheckDomainExistsCommand(testEmail);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.True(response);
    }
}