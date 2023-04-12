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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

internal static class TestEmailPreparationHelper
{
    public static async Task EmailEventsClearNotSentAsync(this MarketParticipantDatabaseFixture fixture)
    {
        await using var context = fixture.DatabaseManager.CreateDbContext();

        await context.EmailEventEntries
            .Where(e => e.Sent == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Sent, x => DateTimeOffset.UtcNow))
            .ConfigureAwait(false);

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public static Task<int> PrepareEmailEventAsync(this MarketParticipantDatabaseFixture fixture)
    {
        return fixture.PrepareEmailEventAsync(TestPreparationEntities.ValidEmailEvent);
    }

    public static async Task<int> PrepareEmailEventAsync(
        this MarketParticipantDatabaseFixture fixture,
        EmailEventEntity emailEventEntity)
    {
        await using var context = fixture.DatabaseManager.CreateDbContext();

        await context.EmailEventEntries.AddAsync(emailEventEntity).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return emailEventEntity.Id;
    }
}
