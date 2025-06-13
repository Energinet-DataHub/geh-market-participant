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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;

public sealed class AdditionalRecipientQueryRepository : IAdditionalRecipientQueryRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public AdditionalRecipientQueryRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<IEnumerable<(ActorNumber ActorNumber, EicFunction MarketRole)>> GetAsync(MeteringPointIdentification meteringPoint)
    {
        var additionalRecipients =
            from additionalRecipient in _marketParticipantDbContext.AdditionalRecipients
            where additionalRecipient.MeteringPoints.Any(mp => mp.MeteringPointIdentification == meteringPoint.Value)
            select additionalRecipient;

        var actors =
            from additionalRecipient in additionalRecipients
            join actor in _marketParticipantDbContext.Actors
                on additionalRecipient.ActorId equals actor.Id
            where actor.Status != ActorStatus.Inactive
            select new { actor.ActorNumber, actor.MarketRole.Function };

        var results = await actors.ToListAsync().ConfigureAwait(false);
        return results.Select(r => (ActorNumber.Create(r.ActorNumber), r.Function));
    }
}
