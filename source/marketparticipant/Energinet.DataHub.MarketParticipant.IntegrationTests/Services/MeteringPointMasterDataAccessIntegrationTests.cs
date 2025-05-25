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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentAssertions.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;
using DatabricksSettings = Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration.DatabricksSettings;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]

public sealed class MeteringPointMasterDataAccessIntegrationTests
{
    private const string ValidGln = "5790000555550";
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly GridAreaId _gridAreaId = new GridAreaId(Guid.NewGuid());

    public MeteringPointMasterDataAccessIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Validate_MeteringPointIsOfOwnedGridArea_ReturnsTrue()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var gridAreaOverviewRepository = await MockGridAreaOverviewRepository();

        host.ServiceCollection.AddScoped<IElectricityMarketClient>(s =>
        {
            var client = s.GetRequiredService<IHttpClientFactory>().CreateClient("ElectricityMarketClient");
            return new ElectricityMarketClient(client);
        });

        var electricityMarketClient = await MockElectricityMarketClient("1234", new List<string>());

        var validationRequest = new MeteringPointMasterDataAccessValidationRequest
        {
            MarketRole = (Authorization.Model.EicFunction)EicFunction.GridAccessProvider,
            MeteringPointId = "TestMeteringPointId"
        };

        var target = new MeteringPointMasterDataAccessValidation(validationRequest, electricityMarketClient, gridAreaOverviewRepository);
        Assert.True(await target.ValidateAsync());
    }

    private async Task<IGridAreaOverviewRepository> MockGridAreaOverviewRepository()
    {
        // arrange
        var gridAreaOverviewItem = new GridAreaOverviewItem(
            _gridAreaId,
            new GridAreaName("name"),
            new GridAreaCode("1234"),
            PriceAreaCode.Dk1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            ActorNumber.Create(ValidGln),
            new ActorName("Test"),
            null,
            null,
            GridAreaType.Distribution);

        var repository = new Mock<IGridAreaOverviewRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(new[] { gridAreaOverviewItem });

        return repository.Object;
    }

    private async Task<IElectricityMarketClient> MockElectricityMarketClient(string meteringPointId, List<string> gridAreas)
    {
        // arrange
        var gridAreaOverviewItem = new GridAreaOverviewItem(
            _gridAreaId,
            new GridAreaName("name"),
            new GridAreaCode("1234"),
            PriceAreaCode.Dk1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            ActorNumber.Create(ValidGln),
            new ActorName("Test"),
            null,
            null,
            GridAreaType.Distribution);

        var repository = new Mock<IElectricityMarketClient>();
        repository.Setup(x => x.GetMeteringPointMasterDataForGridAccessProviderAllowedAsync(meteringPointId, gridAreas)).ReturnsAsync(true);

        return repository.Object;
    }
}
