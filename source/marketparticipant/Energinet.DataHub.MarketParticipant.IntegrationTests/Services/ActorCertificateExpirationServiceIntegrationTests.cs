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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorCertificateExpirationServiceIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public ActorCertificateExpirationServiceIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task CalculateExpirationDateAsync_UnusedCertificate_ExpiresIn1Year()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var testCertificate = MakeTestCertificate(DateTimeOffset.UtcNow.AddYears(2));

        // Act
        var target = scope.ServiceProvider.GetRequiredService<IActorCertificateExpirationService>();
        var actual = await target.CalculateExpirationDateAsync(testCertificate);

        // Assert
        var now = SystemClock.Instance.GetCurrentInstant();
        Assert.InRange(actual, now.Plus(Duration.FromDays(364)), now.Plus(Duration.FromDays(366)));
    }

    [Fact]
    public async Task CalculateExpirationDateAsync_UnusedCertificateExpiresSoon_ReturnsNearestDate()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var testCertificate = MakeTestCertificate(DateTimeOffset.UtcNow.AddDays(2));

        // Act
        var target = scope.ServiceProvider.GetRequiredService<IActorCertificateExpirationService>();
        var actual = await target.CalculateExpirationDateAsync(testCertificate);

        // Assert
        var now = SystemClock.Instance.GetCurrentInstant();
        Assert.Equal(testCertificate.NotAfter, actual.ToDateTimeOffset());
    }

    [Fact]
    public async Task CalculateExpirationDateAsync_ExistingCertificate_ExpiresBasedOnDate()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var addedAt = DateTimeOffset.UtcNow.AddYears(-2);
        var testCertificate = MakeTestCertificate(DateTimeOffset.UtcNow.AddYears(2));

        var actorEntity = await _databaseFixture.PrepareActorAsync();

        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();
        context.UsedActorCertificates.Add(new UsedActorCertificatesEntity
        {
            ActorId = actorEntity.Id,
            Thumbprint = testCertificate.Thumbprint,
            AddedAt = addedAt
        });

        await context.SaveChangesAsync();

        // Act
        var target = scope.ServiceProvider.GetRequiredService<IActorCertificateExpirationService>();
        var actual = await target.CalculateExpirationDateAsync(testCertificate);

        // Assert
        Assert.Equal(addedAt.AddDays(365), actual.ToDateTimeOffset());
    }

    private static X509Certificate2 MakeTestCertificate(DateTimeOffset notAfter)
    {
        using var key = ECDsa.Create();
        var req = new CertificateRequest("cn=datahub.dk", key, HashAlgorithmName.SHA256);
        return req.CreateSelfSigned(DateTimeOffset.Now, notAfter);
    }
}
