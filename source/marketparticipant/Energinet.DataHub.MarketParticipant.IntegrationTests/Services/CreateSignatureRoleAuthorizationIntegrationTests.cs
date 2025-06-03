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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Factories;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
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

        var accessValidatorDispatchService = new Mock<IAccessValidatorDispatchService>();
        accessValidatorDispatchService.Setup(x => x.ValidateAsync(It.IsAny<AccessValidationRequest>()))
            .ReturnsAsync(new AccessValidatorResponse(true, null));
        var request = new MeteringPointMasterDataAccessValidationRequest
        {
            MarketRole = EicFunction.DataHubAdministrator,
            MeteringPointId = "1234",
            ActorNumber = "56789"
        };

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient, _keyClientFixture.KeyName, accessValidatorDispatchService.Object);
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

        var accessValidatorDispatchService = new Mock<IAccessValidatorDispatchService>();
        accessValidatorDispatchService.Setup(x => x.ValidateAsync(It.IsAny<AccessValidationRequest>()))
            .ReturnsAsync(new AccessValidatorResponse(false, null));
        var request = new MeteringPointMasterDataAccessValidationRequest
        {
            MarketRole = EicFunction.GridAccessProvider,
            MeteringPointId = "1234",
            ActorNumber = "56789"
        };

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient, _keyClientFixture.KeyName, accessValidatorDispatchService.Object);

        // assert
        await Assert.ThrowsAsync<ArgumentException>(() => target.CreateSignatureAsync(request, cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task CreateSignatureForMeasurementData_WhenCalledWithRoleDataHubAdministrator_ReturnsSignature()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var accessValidatorDispatchService = new Mock<IAccessValidatorDispatchService>();
        var accessPeriods = new List<AccessPeriod>();
        accessPeriods.Add(new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10)));
        accessPeriods.Add(new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-190), DateTimeOffset.UtcNow.AddDays(-110)));

        accessValidatorDispatchService.Setup(x => x.ValidateAsync(It.IsAny<AccessValidationRequest>()))
            .ReturnsAsync(new AccessValidatorResponse(true, accessPeriods));
        var request = new MeteringPointMeasurementDataAccessValidationRequest
        {
            MarketRole = EicFunction.DataHubAdministrator,
            MeteringPointId = "1234",
            ActorNumber = "56789",
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10))
        };

        // act
        var target = new AuthorizationService(_keyClientFixture.KeyClient, _keyClientFixture.KeyName, accessValidatorDispatchService.Object);
        var actual = await target.CreateSignatureAsync(request, CancellationToken.None);

        // assert
        Assert.NotNull(actual.Value);
        Assert.NotEmpty(actual.Value);
        Assert.NotNull(actual.AccessPeriods);
        if (actual.AccessPeriods != null)
        {
          var accessPeriodsCount = actual.AccessPeriods.Count();
          Assert.Equal(2, accessPeriodsCount);
          var accessPeriod = actual.AccessPeriods.FirstOrDefault(i => i.MeteringPointId == "1234");
          Assert.NotNull(accessPeriod);
          Assert.Equal("1234", accessPeriod.MeteringPointId);
        }
    }
}
