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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class CutoffRepository : ICutoffRepository
{
    private readonly IMarketParticipantDbContext _context;

    public CutoffRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<Instant> GetCutoffAsync(CutoffType type)
    {
        var cutoff = await _context.Cutoffs.SingleOrDefaultAsync(x => x.Type == (int)type).ConfigureAwait(false);

        cutoff ??= await InsertCutoffAsync(type, Instant.FromUnixTimeTicks(0)).ConfigureAwait(false);

        return cutoff.Timestamp.ToInstant();
    }

    public async Task UpdateCutoffAsync(CutoffType type, Instant timestamp)
    {
        var cutoff = await _context.Cutoffs.SingleOrDefaultAsync(x => x.Type == (int)type).ConfigureAwait(false);

        if (cutoff == null)
        {
            await InsertCutoffAsync(type, timestamp).ConfigureAwait(false);
        }
        else
        {
            cutoff.Timestamp = timestamp.ToDateTimeOffset();
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private async Task<CutoffEntity> InsertCutoffAsync(CutoffType type, Instant timestamp)
    {
        var cutoff = new CutoffEntity
        {
            Type = (int)type,
            Timestamp = timestamp.ToDateTimeOffset(),
        };
        _context.Cutoffs.Add(cutoff);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return cutoff;
    }
}
