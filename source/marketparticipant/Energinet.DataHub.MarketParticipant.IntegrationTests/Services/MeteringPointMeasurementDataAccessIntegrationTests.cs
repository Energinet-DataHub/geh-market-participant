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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]

public sealed class MeteringPointMeasurementDataAccessIntegrationTests
{
    private const string ValidGln = "5790000555550";
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly GridAreaId _gridAreaId = new(Guid.NewGuid());

    public MeteringPointMeasurementDataAccessIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Validate_MeteringPointMeasurementDataWhenCalledWithRoleDataHubAdministrator_ReturnsTrue()
    {
        var gridAreaOverviewRepository = MockGridAreaOverviewRepository();

        var service = new Mock<IElectricityMarketClient>();
        service.Setup(x => x.VerifyMeteringPointIsInGridAreaAsync(_gridAreaId.ToString(), new List<string> { "1234" })).ReturnsAsync(true);

        var electricityMarketClient = service.Object;

        var validationRequest = new MeteringPointMeasurementDataAccessValidationRequest
        {
            MarketRole = Authorization.Model.EicFunction.DataHubAdministrator,
            MeteringPointId = "1234",
            ActorNumber = "56789",
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10))
        };

        // Act + Assert
        var target = new MeteringPointMeasurementDataAccessValidation(electricityMarketClient, gridAreaOverviewRepository);

        var accessValidatorResponse = await target.ValidateAsync(validationRequest).ConfigureAwait(true);
        Assert.True(accessValidatorResponse.Valid);
    }

    [Fact]
    public async Task Validate_MeteringPointMeasurementDataWhenCalledWithRoleGridAccessProvider_ReturnsTrueValidate()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var gridAreaOverviewRepository = MockGridAreaOverviewRepository();

        var service = new Mock<IElectricityMarketClient>();
        service.Setup(x => x.VerifyMeteringPointIsInGridAreaAsync("1234", new List<string> { "1234" })).ReturnsAsync(true);

        var electricityMarketClient = service.Object;

        var validationRequest = new MeteringPointMeasurementDataAccessValidationRequest
        {
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10)),
            MarketRole = Authorization.Model.EicFunction.GridAccessProvider,
            MeteringPointId = "1234",
            ActorNumber = ValidGln
        };

        // Act + Assert
        var target = new MeteringPointMeasurementDataAccessValidation(electricityMarketClient, gridAreaOverviewRepository);
        var response = await target.ValidateAsync(validationRequest).ConfigureAwait(true);
        Assert.True(response.Valid);
        Assert.NotNull(response.ValidAccessPeriods);
        Assert.All(response.ValidAccessPeriods, x => Assert.Equal("1234", x.MeteringPointId));
    }

    [Fact]
    public async Task Validate_MeteringPointMeasurementDataWhenCalledWithRoleBalanceSupplier_ReturnsTrueValidate()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var gridAreaOverviewRepository = MockGridAreaOverviewRepository();
        var validationRequest = new MeteringPointMeasurementDataAccessValidationRequest
        {
            RequestedPeriod = new AccessPeriod("1234", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10)),
            MarketRole = Authorization.Model.EicFunction.EnergySupplier,
            MeteringPointId = "1234",
            ActorNumber = ValidGln
        };
        var accessPeriods = new List<AccessPeriod>()
        {
            new("1234", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10)),
            new("1234", DateTimeOffset.UtcNow.AddDays(-190), DateTimeOffset.UtcNow.AddDays(-110))
        };

        var service = new Mock<IElectricityMarketClient>();

        var requestedPeriod = new Interval(Instant.FromDateTimeOffset(validationRequest.RequestedPeriod.FromDate), Instant.FromDateTimeOffset(validationRequest.RequestedPeriod.ToDate));
        service.Setup(x => x.GetSupplierPeriodsAsync("1234", ValidGln, requestedPeriod)).ReturnsAsync(accessPeriods);
        var electricityMarketClient = service.Object;

        // Act + Assert
        var target = new MeteringPointMeasurementDataAccessValidation(electricityMarketClient, gridAreaOverviewRepository);
        var response = await target.ValidateAsync(validationRequest).ConfigureAwait(true);
        Assert.True(response.Valid);
        Assert.NotNull(response.ValidAccessPeriods);
        Assert.All(response.ValidAccessPeriods, x => Assert.Equal("1234", x.MeteringPointId));
    }

    private IGridAreaOverviewRepository MockGridAreaOverviewRepository()
    {
        // arrange
        var gridAreaOverviewItem = new GridAreaOverviewItem(
            _gridAreaId,
            new GridAreaName("name"),
            new Domain.Model.GridAreaCode("1234"),
            PriceAreaCode.Dk1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            ActorNumber.Create(ValidGln),
            new ActorName("Test"),
            null,
            null,
            GridAreaType.Distribution);

        var repository = new Mock<IGridAreaOverviewRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync([gridAreaOverviewItem]);

        return repository.Object;
    }
}
