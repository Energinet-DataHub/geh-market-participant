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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateSignatureHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public CreateSignatureHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task CreateSignature_WhenCalled_ReturnsSignature()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        var meteringPointMasterDataAccess = new MeteringPointMasterDataAccessValidation(EicFunction.DataHubAdministrator);

        var jsonString = JsonSerializer.Serialize<IAccessValidation>(meteringPointMasterDataAccess);
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var accessJsonString = Convert.ToBase64String(jsonBytes);

        // act
        var actual = await target.Send(new CreateSignatureCommand(accessJsonString));

        // assert
        Assert.NotNull(actual);
        Assert.False(string.IsNullOrWhiteSpace(actual.Signature.Signature));
    }

    [Fact]
    public async Task CreateSignature_Wrong_Object_ThrowsException()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        var jsonString = JsonSerializer.Serialize<object>(new object());
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var accessJsonString = Convert.ToBase64String(jsonBytes);

        // act + assert
        await Assert.ThrowsAsync<ArgumentException>(() => target.Send(new CreateSignatureCommand(accessJsonString)));
    }

    [Fact]
    public async Task CreateSignature_Empty_ThrowsException()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act + assert
        await Assert.ThrowsAsync<ArgumentException>(() => target.Send(new CreateSignatureCommand(string.Empty)));
    }
}
