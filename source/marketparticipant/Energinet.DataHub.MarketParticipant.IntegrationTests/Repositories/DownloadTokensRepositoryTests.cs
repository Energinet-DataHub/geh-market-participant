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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class DownloadTokensRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public DownloadTokensRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Download_Token_ReturnsGuid()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var downloadTokensRepository = new DownloadTokenRespository(context);

        // Act
        var id = await downloadTokensRepository.CreateDownloadTokenAsync("accessToken");

        // Assert
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Get_And_Use_Download_Token_Returns_Authorization()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var downloadTokensRepository = new DownloadTokenRespository(context);
        var id = await downloadTokensRepository.CreateDownloadTokenAsync("accessToken");

        // Act
        var authorization = await downloadTokensRepository.GetAndUseDownloadTokenAsync(id);

        // Assert
        Assert.Equal("accessToken", authorization);
    }

    [Fact]
    public async Task Get_And_Use_Download_Token_Twice_Returns_Empty()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var downloadTokensRepository = new DownloadTokenRespository(context);
        var id = await downloadTokensRepository.CreateDownloadTokenAsync("accessToken");

        // Act
        await downloadTokensRepository.GetAndUseDownloadTokenAsync(id);
        var authorization = await downloadTokensRepository.GetAndUseDownloadTokenAsync(id);

        // Assert
        Assert.Equal(authorization, string.Empty);
    }

    [Fact]
    public async Task Get_And_Use_Download_Token_Expired_Returns_Empty()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var downloadTokensRepository = new DownloadTokenRespository(context);
        var id = await downloadTokensRepository.CreateDownloadTokenAsync("accessToken");

        (await context.DownloadTokens.FindAsync(id)).Created = DateTime.UtcNow.AddMinutes(-6);
        await context.SaveChangesAsync();

        // Act
        var authorization = await downloadTokensRepository.GetAndUseDownloadTokenAsync(id);

        // Assert
        Assert.Equal(authorization, string.Empty);
    }
}
