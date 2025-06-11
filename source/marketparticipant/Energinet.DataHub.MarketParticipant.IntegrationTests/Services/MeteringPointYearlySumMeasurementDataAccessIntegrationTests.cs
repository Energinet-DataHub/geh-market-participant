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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;
[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]

public sealed class MeteringPointYearlySumMeasurementDataAccessIntegrationTests
{
    private const string ValidGln = "5790000555550";
    private readonly MarketParticipantDatabaseFixture _fixture;

    public MeteringPointYearlySumMeasurementDataAccessIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Validate_MeteringPointYearlySumMeasurementDataWhenCalledWithRoleDataHubAdministrator_ReturnsFalse()
    {
        var service = new Mock<IElectricityMarketClient>();
        var electricityMarketClient = service.Object;

        var validationRequest = new MeasurementYearlySumAccessValidationRequest
        {
            MarketRole = Authorization.Model.EicFunction.DataHubAdministrator,
            MeteringPointId = "1234",
            ActorNumber = "56789",
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-365), DateTimeOffset.UtcNow.AddDays(-1))
        };

        // Act + Assert
        var target = new MeteringPointYearlySumMeasurementDataAccessValidation(electricityMarketClient);

        var accessValidatorResponse = await target.ValidateAsync(validationRequest).ConfigureAwait(true);
        Assert.False(accessValidatorResponse.Valid);
    }

    [Fact]
    public async Task Validate_MeteringPointYearlySumMeasurementDataWhenCalledWithRoleGridAccessProvider_ReturnsFalseValidate()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var service = new Mock<IElectricityMarketClient>();

        var electricityMarketClient = service.Object;

        var validationRequest = new MeasurementYearlySumAccessValidationRequest
        {
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-365), DateTimeOffset.UtcNow.AddDays(-1)),
            MarketRole = Authorization.Model.EicFunction.GridAccessProvider,
            MeteringPointId = "1234",
            ActorNumber = ValidGln
        };

        // Act + Assert
        var target = new MeteringPointYearlySumMeasurementDataAccessValidation(electricityMarketClient);
        var response = await target.ValidateAsync(validationRequest).ConfigureAwait(true);
        Assert.False(response.Valid);
    }

    [Fact]
    public async Task Validate_MeteringPointMeasurementDataWhenCalledWithRoleBalanceSupplier_ReturnsTrueValidate()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var service = new Mock<IElectricityMarketClient>();

        var electricityMarketClient = service.Object;
        var validationRequest = new MeasurementYearlySumAccessValidationRequest
        {
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-365), DateTimeOffset.UtcNow.AddDays(-1)),
            MarketRole = Authorization.Model.EicFunction.EnergySupplier,
            MeteringPointId = "1234",
            ActorNumber = ValidGln
        };

        // TODO: when implementation is ready (in another task). For now always false is returned.
        var target = new MeteringPointYearlySumMeasurementDataAccessValidation(electricityMarketClient);
        var response = await target.ValidateAsync(validationRequest).ConfigureAwait(true);
        Assert.False(response.Valid);
    }
}
