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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence;

public class AuthorizationDbContext : DbContext, IAuthorizationDbContext
{
    public AuthorizationDbContext(
        DbContextOptions<AuthorizationDbContext> options)
        : base(options)
    {
    }

    // Used for mocking.
    protected AuthorizationDbContext()
    {
    }

    public DbSet<OrganizationEntity> Organizations { get; private set; } = null!;
    public DbSet<ActorEntity> Actors { get; private set; } = null!;
    public DbSet<GridAreaEntity> GridAreas { get; private set; } = null!;
    public DbSet<MarketRoleEntity> MarketRoles { get; private set; } = null!;
    public DbSet<MarketRoleGridAreaEntity> MarketRoleGridAreas { get; private set; } = null!;
    public DbSet<GridAreaLinkEntity> GridAreaLinks { get; private set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.ApplyConfiguration(new MarketRoleGridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaLinkEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
