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
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
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
public sealed class AllowedMarketRoleCombinationsForDelegationRuleServiceIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public AllowedMarketRoleCombinationsForDelegationRuleServiceIntegrationTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task ValidateAsync_AddedMarketRoleIsAllowed_NoException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var messageDelegationRepository = scope.ServiceProvider.GetRequiredService<IMessageDelegationRepository>();

        var delegatedByActorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = EicFunction.EnergySupplier));

        var delegatedBy = await actorRepository.GetAsync(new ActorId(delegatedByActorEntity.Id));
        var delegatedTo = await CreateDelegationOrganizationAsync(EicFunction.BalanceResponsibleParty);

        await CreateTestDelegationAsync(messageDelegationRepository, delegatedBy!, delegatedTo);

        var ruleService = new AllowedMarketRoleCombinationsForDelegationRuleService(actorRepository, messageDelegationRepository);

        // Act + Assert
        await ruleService.ValidateAsync(new OrganizationId(delegatedTo.OrganizationId), EicFunction.EnergySupplier);
    }

    [Fact]
    public async Task ValidateAsync_AddedMarketRoleIsForbidden_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var messageDelegationRepository = scope.ServiceProvider.GetRequiredService<IMessageDelegationRepository>();

        var delegatedByActorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = EicFunction.GridAccessProvider));

        var delegatedBy = await actorRepository.GetAsync(new ActorId(delegatedByActorEntity.Id));
        var delegatedTo = await CreateDelegationOrganizationAsync(EicFunction.BalanceResponsibleParty);

        await CreateTestDelegationAsync(messageDelegationRepository, delegatedBy!, delegatedTo);

        var ruleService = new AllowedMarketRoleCombinationsForDelegationRuleService(actorRepository, messageDelegationRepository);

        // Act + Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => ruleService.ValidateAsync(new OrganizationId(delegatedTo.OrganizationId), EicFunction.EnergySupplier));
        Assert.Equal("message_delegation.market_role_forbidden", exception.Data[ValidationExceptionExtensions.ErrorCodeDataKey]);
    }

    [Fact]
    public async Task ValidateAsync_AddedDelegationIsAllowed_NoException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var messageDelegationRepository = scope.ServiceProvider.GetRequiredService<IMessageDelegationRepository>();

        var gridAreaEntity = await _databaseFixture.PrepareGridAreaAsync();

        var delegatedByActorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = EicFunction.EnergySupplier));

        var delegatedBy = await actorRepository.GetAsync(new ActorId(delegatedByActorEntity.Id));
        var delegatedTo = await CreateDelegationOrganizationAsync(EicFunction.EnergySupplier);

        var delegation = new MessageDelegation(delegatedBy!, DelegationMessageType.Rsm017Inbound);
        delegation.DelegateTo(new ActorId(delegatedTo.Id), new GridAreaId(gridAreaEntity.Id), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));

        var ruleService = new AllowedMarketRoleCombinationsForDelegationRuleService(actorRepository, messageDelegationRepository);

        // Act + Assert
        await ruleService.ValidateAsync(delegation);
    }

    [Fact]
    public async Task ValidateAsync_AddedDelegationIsForbidden_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        await using var context = _databaseFixture.DatabaseManager.CreateDbContext();

        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var messageDelegationRepository = scope.ServiceProvider.GetRequiredService<IMessageDelegationRepository>();

        var gridAreaEntity = await _databaseFixture.PrepareGridAreaAsync();

        var delegatedByActorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = EicFunction.EnergySupplier));

        var delegatedBy = await actorRepository.GetAsync(new ActorId(delegatedByActorEntity.Id));
        var delegatedTo = await CreateDelegationOrganizationAsync(EicFunction.GridAccessProvider);

        var delegation = new MessageDelegation(delegatedBy!, DelegationMessageType.Rsm017Inbound);
        delegation.DelegateTo(new ActorId(delegatedTo.Id), new GridAreaId(gridAreaEntity.Id), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));

        var ruleService = new AllowedMarketRoleCombinationsForDelegationRuleService(actorRepository, messageDelegationRepository);

        // Act + Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => ruleService.ValidateAsync(delegation));
        Assert.Equal("message_delegation.market_role_forbidden", exception.Data[ValidationExceptionExtensions.ErrorCodeDataKey]);
    }

    private async Task<ActorEntity> CreateDelegationOrganizationAsync(EicFunction secondActorEicFunction)
    {
        var organizationEntity = await _databaseFixture.PrepareOrganizationAsync();

        var delegatedActorEntity = await _databaseFixture.PrepareActorAsync(
            organizationEntity,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = EicFunction.Delegated));

        await _databaseFixture.PrepareActorAsync(
            organizationEntity,
            TestPreparationEntities.ValidActiveActor,
            TestPreparationEntities.ValidMarketRole.Patch(marketRole => marketRole.Function = secondActorEicFunction));

        return delegatedActorEntity;
    }

    private async Task CreateTestDelegationAsync(IMessageDelegationRepository messageDelegationRepository, Actor delegatedBy, ActorEntity delegateTo)
    {
        var gridAreaEntity = await _databaseFixture.PrepareGridAreaAsync();

        var delegation = new MessageDelegation(delegatedBy, DelegationMessageType.Rsm017Inbound);
        delegation.DelegateTo(new ActorId(delegateTo.Id), new GridAreaId(gridAreaEntity.Id), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));

        await messageDelegationRepository.AddOrUpdateAsync(delegation);
    }
}
