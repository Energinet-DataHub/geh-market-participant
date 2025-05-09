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
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateSignatureIntegrationTests : IClassFixture<KeyClientFixture>
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly KeyClientFixture _keyClientFixture;

    public CreateSignatureIntegrationTests(MarketParticipantDatabaseFixture databaseFixture, KeyClientFixture keyClientFixture)
    {
        _databaseFixture = databaseFixture;
        _keyClientFixture = keyClientFixture;
    }

    [Fact]
    public async Task CreateSignature_WhenCalledWithGridAccessProvider_ThrowsException()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthorizationService>>();

        var claims = new List<Claim>
        {
            new Claim("marketroles", "GridAccessProvider")
        };

        var securityValidation = new SecurityValidation(claims);

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient.VaultUri, _keyClientFixture.KeyName, logger);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(() => target.CreateSignatureAsync(securityValidation));
    }

    [Fact]
    public async Task CreateSignature_WhenCalledWithDataHubAdministrator_ReturnsSignature()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthorizationService>>();

        var claims = new List<Claim>
        {
            new Claim("marketroles", "DataHubAdministrator")
        };

        var securityValidation = new SecurityValidation(claims);

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient.VaultUri, _keyClientFixture.KeyName, logger);
        var actual = await target.CreateSignatureAsync(securityValidation);

        // assert
        Assert.NotNull(actual);
        Assert.False(string.IsNullOrWhiteSpace(actual.Signature));
    }
}
