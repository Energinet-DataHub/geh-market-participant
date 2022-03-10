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
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class UnitOfWorkTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public UnitOfWorkTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task TestAsync(bool commitUnitOfWork, bool entityCreated)
        {
            // arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            var repository = CreateRepository(context);
            var entity = CreateEntity();
            OrganizationId id = null!;

            //act
            await ExecuteInUnitOfWork(context, commitUnitOfWork, async () =>
            {
                id = await repository.AddOrUpdateAsync(entity).ConfigureAwait(false);
            }).ConfigureAwait(false);

            // assert
            await using var newContext = _fixture.DatabaseManager.CreateDbContext();
            var newRepository = CreateRepository(newContext);
            var actualEntityCreated = await newRepository.GetAsync(id).ConfigureAwait(false) != null;
            Assert.Equal(entityCreated, actualEntityCreated);
        }

        private static async Task ExecuteInUnitOfWork(DbContext context, bool commit, Func<Task> work)
        {
            await using var uow = new UnitOfWork(context);
            await uow.InitializeAsync().ConfigureAwait(false);
            await work().ConfigureAwait(false);
            if (commit)
            {
                await uow.CommitAsync().ConfigureAwait(false);
            }
        }

        private static OrganizationRepository CreateRepository(Infrastructure.Persistence.MarketParticipantDbContext context)
        {
            return new OrganizationRepository(context);
        }

        private static Organization CreateEntity()
        {
            return new Organization("Test");
        }
    }
}
