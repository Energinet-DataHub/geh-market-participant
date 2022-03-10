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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class DomainEventRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public DomainEventRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InsertAsync_RequiredDataSpecified_InsertsEvent()
        {
            // arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var repository = new DomainEventRepository(context);

            // act
            var id = await repository
                .InsertAsync(new DomainEvent(Guid.NewGuid(), nameof(Organization), new OrganizationChangedIntegrationEvent { OrganizationId = Guid.NewGuid(), ActorId = Guid.NewGuid(), Gln = "gln", Name = "name" }))
                .ConfigureAwait(false);

            // assert
            var actual = await Find(id).ConfigureAwait(false);
            Assert.NotNull(actual);
        }

        [Fact]
        public async Task UpdateAsync_Updates()
        {
            // arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var repository = new DomainEventRepository(context);
            var id = await repository
                .InsertAsync(new DomainEvent(
                    Guid.NewGuid(),
                    nameof(Organization),
                    new OrganizationChangedIntegrationEvent { OrganizationId = Guid.NewGuid(), ActorId = Guid.NewGuid(), Gln = "gln", Name = "name" }))
                .ConfigureAwait(false);
            DomainEvent domainEvent = null!;
            await foreach (var e in repository.GetOldestUnsentDomainEventsAsync(100).ConfigureAwait(false))
            {
                if (e.Id == id)
                    domainEvent = e;
            }

            // act
            domainEvent.MarkAsSent();
            await repository.UpdateAsync(domainEvent).ConfigureAwait(false);
            var actual = await Find(id).ConfigureAwait(false);

            // assert
            Assert.True(actual!.IsSent);
        }

        [Fact]
        public async Task GetAsync_UnsentExists_ReturnsUnsent()
        {
            // arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var repository = new DomainEventRepository(context);
            var domainEvent = new DomainEvent(Guid.NewGuid(), nameof(Organization), new OrganizationChangedIntegrationEvent { OrganizationId = Guid.NewGuid(), ActorId = Guid.NewGuid(), Gln = "gln", Name = "name" });
            await repository.InsertAsync(domainEvent).ConfigureAwait(false);

            // act
            var actual = new List<DomainEvent>();
            await foreach (var x in repository.GetOldestUnsentDomainEventsAsync(1))
            {
                actual.Add(x);
            }

            // assert
            Assert.NotNull(actual.FirstOrDefault(x => x.DomainObjectId == domainEvent.DomainObjectId));
        }

        public async Task<DomainEvent?> Find(DomainEventId id)
        {
            await using var newHost = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var newScope = newHost.BeginScope();
            await using var newContext = _fixture.DatabaseManager.CreateDbContext();

            var newRepository = new DomainEventRepository(newContext);

            DomainEvent? actual = null;

            await foreach (var e in newRepository.GetOldestUnsentDomainEventsAsync(100).ConfigureAwait(false))
            {
                if (e.Id == id)
                    actual = e;
            }

            return actual;
        }
    }
}
