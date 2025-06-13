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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.AdditionalRecipients;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.DataApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetAdditionalRecipientsOfMeteringPointHandlerTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetAdditionalRecipientsOfMeteringPointHandlerTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task GetAdditionalRecipientsOfMeteringPoint_WhenCalled_ReturnsRecipients()
    {
        // Arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();
        var expectedMeteringPoint = MockedMeteringPointIdentifier.New();

        await using var dbContext = _databaseFixture.DatabaseManager.CreateDbContext();
        await dbContext.AdditionalRecipients.AddAsync(new AdditionalRecipientEntity
        {
            ActorId = expectedActor.Id,
            MeteringPoints = { new AdditionalRecipientOfMeteringPointEntity { MeteringPointIdentification = expectedMeteringPoint.Value } }
        });

        await dbContext.SaveChangesAsync();

        await using var host = await DataApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var actual = await target.Send(new GetAdditionalRecipientsOfMeteringPointCommand(expectedMeteringPoint.Value));

        // Assert
        Assert.Equal([new AdditionalRecipientDto(expectedActor.ActorNumber, "GridAccessProvider")], actual.Recipients);
    }
}
