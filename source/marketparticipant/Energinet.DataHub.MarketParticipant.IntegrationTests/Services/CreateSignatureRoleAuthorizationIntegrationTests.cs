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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Factories;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CreateSignatureRoleAuthorizationIntegrationTests : IClassFixture<KeyClientFixture>
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly KeyClientFixture _keyClientFixture;

    public CreateSignatureRoleAuthorizationIntegrationTests(MarketParticipantDatabaseFixture databaseFixture, KeyClientFixture keyClientFixture)
    {
        _databaseFixture = databaseFixture;
        _keyClientFixture = keyClientFixture;
    }

    [Fact]
    public async Task CreateSignature_WhenCalledWithRoleDataHubAdministrator_ReturnsSignature()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = new Mock<ILogger<AuthorizationService>>().Object;
        var validatorFactory = new Mock<IAccessValidatorFactory>().Object;

        var request = new MeteringPointMasterDataAccessValidationRequest
        {
            MarketRole = EicFunction.DataHubAdministrator,
            MeteringPointId = "1234"
        };

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient, _keyClientFixture.KeyName, logger, validatorFactory);
        var actual = await target.CreateSignatureAsync(request, CancellationToken.None);

        // assert
        Assert.NotNull(actual.Value);
        Assert.NotEmpty(actual.Value);
    }

    [Fact]
    public async Task CreateSignature_WhenCalledWithRoleGridAccessProvider_ThrowsException()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var logger = new Mock<ILogger<AuthorizationService>>().Object;
        var validatorFactory = new Mock<IAccessValidatorFactory>().Object;

        var request = new MeteringPointMasterDataAccessValidationRequest
        {
            MarketRole = EicFunction.GridAccessProvider,
            MeteringPointId = "1234"
        };

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient, _keyClientFixture.KeyName, logger, validatorFactory);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(() => target.CreateSignatureAsync(request, cancellationToken: CancellationToken.None));
    }
}
