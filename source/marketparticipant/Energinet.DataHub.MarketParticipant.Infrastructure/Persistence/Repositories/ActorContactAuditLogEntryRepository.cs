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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class ActorContactAuditLogEntryRepository : IActorContactAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public ActorContactAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ActorContactAuditLogEntry>> GetAsync(ActorId actor)
        {
            var historicEntities = await _context.ActorContacts
                .ReadAllHistoryForAsync(entity => entity.ActorId == actor.Value && entity.Category == ContactCategory.Default)
                .ConfigureAwait(false);

            var auditedProperties = new[]
            {
                new
                {
                    Property = ActorContactChangeType.Name,
                    ReadValue = new Func<ActorContactEntity, object?>(entity => entity.Name ?? string.Empty)
                },
                new
                {
                    Property = ActorContactChangeType.Email,
                    ReadValue = new Func<ActorContactEntity, object?>(entity => entity.Email ?? string.Empty)
                },
                new
                {
                    Property = ActorContactChangeType.Phone,
                    ReadValue = new Func<ActorContactEntity, object?>(entity => entity.Phone ?? string.Empty)
                },
            };

            var auditEntries = new List<ActorContactAuditLogEntry>();

            for (var i = 0; i < historicEntities.Count; i++)
            {
                var isFirst = i == 0;
                var current = historicEntities[i];
                var previous = isFirst ? current : historicEntities[i - 1];

                if (current.Entity.DeletedByIdentityId == null)
                {
                    auditEntries.Add(new ActorContactAuditLogEntry(
                        actor,
                        new AuditIdentity(current.Entity.ChangedByIdentityId),
                        ActorContactChangeType.Deleted,
                        current.Entity.Category,
                        current.PeriodStart,
                        string.Empty));
                }
                else
                {
                    foreach (var auditedProperty in auditedProperties)
                    {
                        var currentValue = auditedProperty.ReadValue(current.Entity);
                        var previousValue = auditedProperty.ReadValue(previous.Entity);

                        if (!Equals(currentValue, previousValue) || isFirst)
                        {
                            auditEntries.Add(new ActorContactAuditLogEntry(
                                actor,
                                new AuditIdentity(current.Entity.ChangedByIdentityId),
                                auditedProperty.Property,
                                current.Entity.Category,
                                current.PeriodStart,
                                currentValue?.ToString() ?? string.Empty));
                        }
                    }
                }
            }

            return auditEntries.OrderBy(entry => entry.Timestamp).ToList();
        }
    }
}
