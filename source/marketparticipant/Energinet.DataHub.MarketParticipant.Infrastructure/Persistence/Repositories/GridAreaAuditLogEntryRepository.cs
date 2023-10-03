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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class GridAreaAuditLogEntryRepository : IGridAreaAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public GridAreaAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GridAreaAuditLogEntry>> GetAsync(GridAreaId gridAreaId)
        {
            var historicEntities = await _context.GridAreas
                .ReadAllHistoryForAsync(entity => entity.Id == gridAreaId.Value)
                .ConfigureAwait(false);

            var auditedProperties = new[]
            {
                new
                {
                    Property = GridAreaAuditLogEntryField.Name,
                    ReadValue = new Func<GridAreaEntity, object?>(entity => entity.Name)
                },
            };

            var auditEntries = new List<GridAreaAuditLogEntry>();

            for (var i = 1; i < historicEntities.Count; i++)
            {
                var current = historicEntities[i];
                var previous = historicEntities[i - 1];

                foreach (var auditedProperty in auditedProperties)
                {
                    var currentValue = auditedProperty.ReadValue(current.Entity);
                    var previousValue = auditedProperty.ReadValue(previous.Entity);

                    if (!Equals(currentValue, previousValue))
                    {
                        auditEntries.Add(new GridAreaAuditLogEntry(
                            current.PeriodStart,
                            new AuditIdentity(current.Entity.ChangedByIdentityId),
                            auditedProperty.Property,
                            previousValue?.ToString() ?? string.Empty,
                            currentValue?.ToString() ?? string.Empty,
                            new GridAreaId(current.Entity.Id)));
                    }
                }
            }

            return auditEntries;
        }
    }
}
