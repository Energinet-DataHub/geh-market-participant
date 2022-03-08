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
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class DomainEventRepository : IDomainEventRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public DomainEventRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task InsertAsync(Guid domainObjectId, string domainObjectType, object domainEvent)
        {
            using var ms = new MemoryStream();

            await JsonSerializer.SerializeAsync(
                ms,
                domainEvent,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).ConfigureAwait(false);

            var entity = new DomainEventEntity
            {
                EntityId = domainObjectId,
                EntityType = domainObjectType,
                Timestamp = DateTimeOffset.UtcNow,
                Event = Encoding.UTF8.GetString(ms.ToArray())
            };

            await _context.DomainEvents.AddAsync(entity).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
