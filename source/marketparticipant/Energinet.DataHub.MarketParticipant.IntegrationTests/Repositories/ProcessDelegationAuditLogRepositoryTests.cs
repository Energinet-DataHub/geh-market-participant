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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ProcessDelegationAuditLogRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ProcessDelegationAuditLogRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_StartStopDelegation_HasCorrectAuditLogs()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        await using var scope = host.BeginScope();
        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var actorAuditLogEntryRepository = scope.ServiceProvider.GetRequiredService<IProcessDelegationAuditLogRepository>();

        var delegator = await actorRepository.GetAsync(new ActorId((await _fixture.PrepareActiveActorAsync()).Id));
        var delegated = await actorRepository.GetAsync(new ActorId((await _fixture.PrepareActiveActorAsync()).Id));

        var baseDateTime = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);

        var processDelegation = new ProcessDelegation(delegator!, DelegatedProcess.RequestWholesaleResults);
        processDelegation.DelegateTo(delegated!.Id, new GridAreaId((await _fixture.PrepareGridAreaAsync()).Id), baseDateTime);

        var processDelegationRepository = scope.ServiceProvider.GetRequiredService<IProcessDelegationRepository>();
        var processDelegationId = await processDelegationRepository.AddOrUpdateAsync(processDelegation);

        var freshMessageDelegation = await processDelegationRepository.GetAsync(processDelegationId);
        freshMessageDelegation!.StopDelegation(freshMessageDelegation.Delegations.Single(), baseDateTime.Plus(Duration.FromDays(2)));
        await processDelegationRepository.AddOrUpdateAsync(freshMessageDelegation);

        // act
        var actual = (await actorAuditLogEntryRepository.GetAsync(delegator!.Id)).Where(x => new[]
        {
            ActorAuditedChange.DelegationStart,
            ActorAuditedChange.DelegationStop,
        }.Contains(x.Change)).ToArray();

        // assert
        Assert.NotNull(actual.SingleOrDefault(x => x.Change == ActorAuditedChange.DelegationStart));
        Assert.NotNull(actual.SingleOrDefault(x => x.Change == ActorAuditedChange.DelegationStop));
    }
}
