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
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateSignatureHandlerIntegrationTests : IClassFixture<KeyClientFixture>
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly KeyClientFixture _keyClientFixture;

    public CreateSignatureHandlerIntegrationTests(MarketParticipantDatabaseFixture databaseFixture, KeyClientFixture keyClientFixture)
    {
        _databaseFixture = databaseFixture;
        _keyClientFixture = keyClientFixture;
    }

    [Fact]
    public async Task CreateSignature_WhenCalledWithDataHubAdministrator_ReturnsSignature()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        var signingClient = new SigningKeyRing(
           SystemClock.Instance,
           _keyClientFixture.KeyClient,
           _keyClientFixture.KeyName);

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthorizationService>>();
        var meteringPointMasterDataAccess = new MeteringPointMasterDataAccessValidation(EicFunction.DataHubAdministrator);

        var jsonString = JsonSerializer.Serialize<IAccessValidation>(meteringPointMasterDataAccess);
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var accessJsonString = Convert.ToBase64String(jsonBytes);

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient.VaultUri, _keyClientFixture.KeyName, logger);
        var actual = await target.CreateSignatureAsync(accessJsonString);

        // assert
        Assert.NotNull(actual);
        Assert.False(string.IsNullOrWhiteSpace(actual.Signature));
    }

    [Fact]
    public async Task CreateSignature_WhenCalledWithNotDataHubAdministrator_ThrowsException()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        var signingClient = new SigningKeyRing(
           SystemClock.Instance,
           _keyClientFixture.KeyClient,
           _keyClientFixture.KeyName);

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthorizationService>>();
        var meteringPointMasterDataAccess = new MeteringPointMasterDataAccessValidation(EicFunction.BalanceResponsibleParty);

        var jsonString = JsonSerializer.Serialize<IAccessValidation>(meteringPointMasterDataAccess);
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var accessJsonString = Convert.ToBase64String(jsonBytes);

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient.VaultUri, _keyClientFixture.KeyName, logger);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(() => target.CreateSignatureAsync(accessJsonString));
    }

    [Fact]
    public async Task CreateSignature_Wrong_Object_ThrowsException()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        var signingClient = new SigningKeyRing(
         SystemClock.Instance,
         _keyClientFixture.KeyClient,
         _keyClientFixture.KeyName);

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthorizationService>>();

        var jsonString = JsonSerializer.Serialize<object>(new object());
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var accessJsonString = Convert.ToBase64String(jsonBytes);

        // act + assert
        var target = new AuthorizationService(_keyClientFixture.KeyClient.VaultUri, _keyClientFixture.KeyName, logger);
        await Assert.ThrowsAsync<ArgumentException>(() => target.CreateSignatureAsync(accessJsonString));
    }

    [Fact]
    public async Task CreateSignature_Empty_ThrowsException()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();

        var signingClient = new SigningKeyRing(
        SystemClock.Instance,
        _keyClientFixture.KeyClient,
        _keyClientFixture.KeyName);

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthorizationService>>();

        // act + assert
        var target = new AuthorizationService(_keyClientFixture.KeyClient.VaultUri, _keyClientFixture.KeyName, logger);
        await Assert.ThrowsAsync<ArgumentException>(() => target.CreateSignatureAsync(string.Empty));
    }
}
