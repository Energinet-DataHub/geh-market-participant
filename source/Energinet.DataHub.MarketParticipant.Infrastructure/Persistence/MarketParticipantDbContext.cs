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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.EntityFrameworkCore;
using SmartEnum.EFCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence
{
    public class MarketParticipantDbContext : DbContext, IMarketParticipantDbContext
    {
        public MarketParticipantDbContext(DbContextOptions<MarketParticipantDbContext> options)
            : base(options)
        {
        }

        // Used for mocking.
        protected MarketParticipantDbContext()
        {
        }

        public DbSet<OrganizationEntity> Organizations { get; private set; } = null!;
        public DbSet<GridAreaEntity> GridAreas { get; private set; } = null!;
        public DbSet<DomainEventEntity> DomainEvents { get; private set; } = null!;

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Guard.ThrowIfNull(modelBuilder, nameof(modelBuilder));
            modelBuilder.ApplyConfiguration(new OrganizationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizationRoleEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MarketRoleEntityConfiguration());
            modelBuilder.ApplyConfiguration(new GridAreEntityConfiguration());
            modelBuilder.ApplyConfiguration(new DomainEventEntityConfiguration());
            modelBuilder.ConfigureSmartEnum();
            base.OnModelCreating(modelBuilder);
        }
    }
}
