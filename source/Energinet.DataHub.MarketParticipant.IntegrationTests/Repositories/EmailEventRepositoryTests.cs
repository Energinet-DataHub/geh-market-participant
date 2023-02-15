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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.ActorIntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class EmailEventRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public EmailEventRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InsertsEmailEvent()
        {
            // arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var emailEventRepository = new EmailEventRepository(context);

            #pragma warning disable CA5394
            var emailRandom = new EmailAddress(Random.Shared.Next(444444) + "fakeemail@fake.dk");
            #pragma warning restore CA5394
            var newEmailEvent = new EmailEvent(emailRandom, EmailEventType.UserInvite);

            // act
            await emailEventRepository.InsertAsync(newEmailEvent).ConfigureAwait(false);

            // assert
            var emailEventRepository2 = new EmailEventRepository(context2);
            var savedEvents = await emailEventRepository2.GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite);
            Assert.Single(savedEvents, e => e.Email.Equals(emailRandom));
        }

        [Fact]
        public async Task MarkAsSent()
        {
            // arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context1 = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();
            await using var context3 = _fixture.DatabaseManager.CreateDbContext();

            var emailEventRepository1 = new EmailEventRepository(context1);

            #pragma warning disable CA5394
            var emailRandom = new EmailAddress(Random.Shared.Next(444444) + "fakeemail@fake.dk");
            #pragma warning restore CA5394
            var newEmailEvent = new EmailEvent(emailRandom, EmailEventType.UserInvite);

            // act
            await emailEventRepository1.InsertAsync(newEmailEvent).ConfigureAwait(false);

            var emailEventRepository2 = new EmailEventRepository(context2);
            var savedEvents = (await emailEventRepository2
                .GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite).ConfigureAwait(false))
                .ToList();
            var elementToMarkAsSent = savedEvents.Single(e => e.Email.Equals(emailRandom));
            await emailEventRepository2.MarkAsSentAsync(elementToMarkAsSent).ConfigureAwait(false);

            // assert
            var emailEventRepository3 = new EmailEventRepository(context3);
            var savedEventsSent = (await emailEventRepository3
                    .GetAllEmailEventByTypeAsync(EmailEventType.UserInvite)
                    .ConfigureAwait(false))
                .ToList();
            Assert.Single(savedEventsSent, e => e.Email.Equals(emailRandom) && e.Sent != null);
        }
    }
}
