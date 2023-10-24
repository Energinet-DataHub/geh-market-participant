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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorContactRepository : IActorContactRepository
{
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public ActorContactRepository(
        IMarketParticipantDbContext marketParticipantDbContext,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _auditIdentityProvider = auditIdentityProvider;
    }

    public async Task<ActorContact?> GetAsync(ContactId contactId)
    {
        ArgumentNullException.ThrowIfNull(contactId, nameof(contactId));

        var contact = await _marketParticipantDbContext.ActorContacts
            .FindAsync(contactId.Value)
            .ConfigureAwait(false);

        return contact is null ? null : ActorContactMapper.MapFromEntity(contact);
    }

    public async Task<IEnumerable<ActorContact>> GetAsync(ActorId actorId)
    {
        ArgumentNullException.ThrowIfNull(actorId);

        var query =
            from contact in _marketParticipantDbContext.ActorContacts
            where contact.ActorId == actorId.Value
            select contact;

        var entities = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return entities.Select(ActorContactMapper.MapFromEntity);
    }

    public async Task<ContactId> AddAsync(ActorContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact, nameof(contact));

        var destination = new ActorContactEntity();
        ActorContactMapper.MapToEntity(contact, destination);
        _marketParticipantDbContext.ActorContacts.Update(destination);

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        return new ContactId(destination.Id);
    }

    public async Task RemoveAsync(ActorContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact, nameof(contact));

        var entity = await _marketParticipantDbContext
            .ActorContacts
            .FindAsync(contact.Id.Value)
            .ConfigureAwait(false);

        if (entity == null)
            return;

        await _marketParticipantDbContext
            .ActorContacts
            .Where(ac => ac.Id == entity.Id)
            .ExecuteUpdateAsync(props => props.SetProperty(
                ace =>
                    ace.DeletedByIdentityId,
                _auditIdentityProvider.IdentityId.Value))
            .ConfigureAwait(false);

        _marketParticipantDbContext.ActorContacts.Remove(entity);

        await _marketParticipantDbContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }
}
