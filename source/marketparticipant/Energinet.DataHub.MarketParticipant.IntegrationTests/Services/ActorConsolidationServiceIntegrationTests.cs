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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorConsolidationServiceIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public ActorConsolidationServiceIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task ConsolidateAsync_GridAreasAreTransferred_NoException()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var actorConsolidationAuditLogRepository = scope.ServiceProvider.GetRequiredService<IActorConsolidationAuditLogRepository>();
        var actorCredentialsRemovalService = scope.ServiceProvider.GetRequiredService<IActorCredentialsRemovalService>();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var gridAreaRepository = scope.ServiceProvider.GetRequiredService<IGridAreaRepository>();

        var auditIdentityProvider = new Mock<IAuditIdentityProvider>();
        auditIdentityProvider.Setup(repo => repo.IdentityId).Returns(new Domain.Model.Users.AuditIdentity(Guid.NewGuid()));
        host.ServiceCollection.RemoveAll<IAuditIdentityProvider>();
        host.ServiceCollection.AddScoped(_ => auditIdentityProvider.Object);

        var domainEventRepository = new Mock<IDomainEventRepository>();
        host.ServiceCollection.RemoveAll<IDomainEventRepository>();
        host.ServiceCollection.AddScoped(_ => domainEventRepository.Object);

        var gridArea1Entity = await _databaseFixture.PrepareGridAreaAsync();
        var gridArea2Entity = await _databaseFixture.PrepareGridAreaAsync();

        var fromActorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole =>
            {
                marketRole.Function = EicFunction.GridAccessProvider;
                marketRole.GridAreas.Add(new MarketRoleGridAreaEntity { GridAreaId = gridArea1Entity.Id });
                marketRole.GridAreas.Add(new MarketRoleGridAreaEntity { GridAreaId = gridArea2Entity.Id });
            }));

        var toActorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = EicFunction.GridAccessProvider));

        var actorConsolidationService = new ActorConsolidationService(
            actorConsolidationAuditLogRepository,
            actorCredentialsRemovalService,
            actorRepository,
            auditIdentityProvider.Object,
            domainEventRepository.Object,
            gridAreaRepository);
        var scheduledAt = Instant.FromUtc(2024, 1, 1, 10, 59);

        // Act
        await actorConsolidationService.ConsolidateAsync(new ActorConsolidation(new ActorId(fromActorEntity.Id), new ActorId(toActorEntity.Id), scheduledAt));

        // Assert
        var fromActor = await actorRepository.GetAsync(new ActorId(fromActorEntity.Id));
        Assert.NotNull(fromActor);
        Assert.Equal(ActorStatus.Inactive, fromActor.Status);
        Assert.Empty(fromActor.MarketRole.GridAreas);

        var toActor = await actorRepository.GetAsync(new ActorId(toActorEntity.Id));
        Assert.NotNull(toActor);
        Assert.Equal(2, toActor.MarketRole.GridAreas.Count);

        var gridAreas = await gridAreaRepository.GetAsync();
        Assert.All(gridAreas, result => Assert.Equal(scheduledAt.ToDateTimeOffset(), result.ValidTo));
    }
}
