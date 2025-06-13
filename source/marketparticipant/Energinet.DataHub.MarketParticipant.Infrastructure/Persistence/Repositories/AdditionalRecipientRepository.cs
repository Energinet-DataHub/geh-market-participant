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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class AdditionalRecipientRepository : IAdditionalRecipientRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public AdditionalRecipientRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<AdditionalRecipient?> GetAsync(ActorId actorId)
    {
        var result = await _marketParticipantDbContext.AdditionalRecipients
            .FirstOrDefaultAsync(ar => ar.ActorId == actorId.Value)
            .ConfigureAwait(false);

        return result?.ToDomainModel();
    }

    public async Task AddOrUpdateAsync(AdditionalRecipient additionalRecipient)
    {
        ArgumentNullException.ThrowIfNull(additionalRecipient);

        AdditionalRecipientEntity entity;

        var entityKey = additionalRecipient.Id.Value;
        if (entityKey == 0)
        {
            entity = new AdditionalRecipientEntity();

            await _marketParticipantDbContext.AdditionalRecipients
                .AddAsync(entity)
                .ConfigureAwait(false);
        }
        else
        {
            entity = await _marketParticipantDbContext.AdditionalRecipients
                .FindAsync(entityKey)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"AdditionalRecipient with id {entityKey} is missing, even though it cannot be deleted.");
        }

        entity.PatchFromDomainModel(additionalRecipient);

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }
}
