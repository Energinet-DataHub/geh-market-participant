﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;

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
    public DbSet<ActorEntity> Actors { get; private set; } = null!;
    public DbSet<GridAreaEntity> GridAreas { get; private set; } = null!;
    public DbSet<MarketRoleEntity> MarketRoles { get; private set; } = null!;
    public DbSet<MarketRoleGridAreaEntity> MarketRoleGridAreas { get; private set; } = null!;
    public DbSet<ActorContactEntity> ActorContacts { get; private set; } = null!;
    public DbSet<GridAreaLinkEntity> GridAreaLinks { get; private set; } = null!;
    public DbSet<UniqueActorMarketRoleGridAreaEntity> UniqueActorMarketRoleGridAreas { get; private set; } = null!;
    public DbSet<GridAreaAuditLogEntryEntity> GridAreaAuditLogEntries { get; private set; } = null!;
    public DbSet<ActorSynchronizationEntity> ActorSynchronizationEntries { get; private set; } = null!;
    public DbSet<UserEntity> Users { get; private set; } = null!;
    public DbSet<UserRoleAssignmentEntity> UserRoleAssignments { get; private set; } = null!;
    public DbSet<UserRoleEntity> UserRoles { get; private set; } = null!;
    public DbSet<UserRoleAssignmentAuditLogEntryEntity> UserRoleAssignmentAuditLogEntries { get; private set; } = null!;
    public DbSet<UserRoleAuditLogEntryEntity> UserRoleAuditLogEntries { get; private set; } = null!;
    public DbSet<UserInviteAuditLogEntryEntity> UserInviteAuditLogEntries { get; private set; } = null!;
    public DbSet<PermissionEntity> Permissions { get; private set; } = null!;
    public DbSet<PermissionAuditLogEntryEntity> PermissionAuditLogEntries { get; private set; } = null!;

    public DbSet<EmailEventEntity> EmailEventEntries { get; private set; } = null!;

    public Task<int> SaveChangesAsync()
    {
        return base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.ApplyConfiguration(new OrganizationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketRoleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketRoleGridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaLinkEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorContactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UniqueActorMarketRoleGridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorSynchronizationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleAssignmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEicFunctionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRolePermissionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleAssignmentAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserInviteAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailEventEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}