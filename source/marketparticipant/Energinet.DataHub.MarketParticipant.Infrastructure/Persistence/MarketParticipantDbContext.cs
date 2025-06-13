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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Audit;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;

public class MarketParticipantDbContext : DbContext, IMarketParticipantDbContext
{
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private bool _savingChanges;

    public MarketParticipantDbContext(
        DbContextOptions<MarketParticipantDbContext> options,
        IAuditIdentityProvider auditIdentityProvider)
        : base(options)
    {
        _auditIdentityProvider = auditIdentityProvider;

        // ReSharper disable VirtualMemberCallInConstructor // Follows MS example.
        ChangeTracker.Tracked += (_, e) => OnEntityStateChanged(e.Entry);
        ChangeTracker.StateChanged += (_, e) => OnEntityStateChanged(e.Entry);
    }

    // Used for mocking.
    protected MarketParticipantDbContext()
    {
        _auditIdentityProvider = KnownAuditIdentityProvider.TestFramework;

        // ReSharper disable VirtualMemberCallInConstructor // Follows MS example.
        ChangeTracker.Tracked += (_, e) => OnEntityStateChanged(e.Entry);
        ChangeTracker.StateChanged += (_, e) => OnEntityStateChanged(e.Entry);
    }

    public DbSet<DownloadTokenEntity> DownloadTokens { get; private set; } = null!;
    public DbSet<OrganizationEntity> Organizations { get; private set; } = null!;
    public DbSet<ActorEntity> Actors { get; private set; } = null!;
    public DbSet<GridAreaEntity> GridAreas { get; private set; } = null!;
    public DbSet<MarketRoleEntity> MarketRoles { get; private set; } = null!;
    public DbSet<MarketRoleGridAreaEntity> MarketRoleGridAreas { get; private set; } = null!;
    public DbSet<ActorContactEntity> ActorContacts { get; private set; } = null!;
    public DbSet<GridAreaLinkEntity> GridAreaLinks { get; private set; } = null!;
    public DbSet<UniqueActorMarketRoleGridAreaEntity> UniqueActorMarketRoleGridAreas { get; private set; } = null!;
    public DbSet<UserEntity> Users { get; private set; } = null!;
    public DbSet<UserRoleAssignmentEntity> UserRoleAssignments { get; private set; } = null!;
    public DbSet<UserRoleEntity> UserRoles { get; private set; } = null!;
    public DbSet<UserRoleAssignmentAuditLogEntryEntity> UserRoleAssignmentAuditLogEntries { get; private set; } = null!;
    public DbSet<UserRolePermissionEntity> UserRolePermissionEntries { get; private set; } = null!;
    public DbSet<UserInviteAuditLogEntryEntity> UserInviteAuditLogEntries { get; private set; } = null!;
    public DbSet<UserIdentityAuditLogEntryEntity> UserIdentityAuditLogEntries { get; private set; } = null!;
    public DbSet<PermissionEntity> Permissions { get; private set; } = null!;
    public DbSet<DomainEventEntity> DomainEvents { get; private set; } = null!;
    public DbSet<EmailEventEntity> EmailEventEntries { get; private set; } = null!;
    public DbSet<ActorCertificateCredentialsEntity> ActorCertificateCredentials { get; private set; } = null!;
    public DbSet<ActorClientSecretCredentialsEntity> ActorClientSecretCredentials { get; private set; } = null!;
    public DbSet<UsedActorCertificatesEntity> UsedActorCertificates { get; private set; } = null!;
    public DbSet<ProcessDelegationEntity> ProcessDelegations { get; private set; } = null!;
    public DbSet<DelegationPeriodEntity> DelegationPeriods { get; private set; } = null!;
    public DbSet<BalanceResponsibilityRequestEntity> BalanceResponsibilityRequests { get; private set; } = null!;
    public DbSet<BalanceResponsibilityRelationEntity> BalanceResponsibilityRelations { get; private set; } = null!;
    public DbSet<CutoffEntity> Cutoffs { get; private set; } = null!;
    public DbSet<OrganizationDomainEntity> OrganizationDomains { get; private set; } = null!;
    public DbSet<ActorConsolidationEntity> ActorConsolidations { get; private set; } = null!;
    public DbSet<ActorConsolidationAuditLogEntryEntity> ActorConsolidationAuditLogEntries { get; private set; } = null!;
    public DbSet<AdditionalRecipientEntity> AdditionalRecipients { get; private set; } = null!;

    public async Task<int> SaveChangesAsync()
    {
        var hasExternalTransaction = Database.CurrentTransaction != null;
        int affected;

        try
        {
            _savingChanges = true;
            affected = await base.SaveChangesAsync().ConfigureAwait(false);
        }
        finally
        {
            _savingChanges = false;
        }

        if (!hasExternalTransaction)
        {
            var contextCreatedTransaction = Database.CurrentTransaction;
            if (contextCreatedTransaction != null)
            {
                await contextCreatedTransaction.CommitAsync().ConfigureAwait(false);
            }
        }

        return affected;
    }

    public async Task CreateLockAsync(LockableEntity lockableEntity)
    {
        ArgumentNullException.ThrowIfNull(lockableEntity);

        if (Database.CurrentTransaction == null)
            throw new InvalidOperationException("A transaction is required");

#pragma warning disable EF1002
        await Database.ExecuteSqlRawAsync($"SELECT TOP 0 NULL FROM [{lockableEntity.Name}] WITH (TABLOCKX)").ConfigureAwait(false);
#pragma warning restore EF1002
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.ApplyConfiguration(new DownloadTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketRoleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketRoleGridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaLinkEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorContactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UniqueActorMarketRoleGridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleAssignmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEicFunctionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRolePermissionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleAssignmentAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserInviteAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserIdentityAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DomainEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorCertificateCredentialsEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorClientSecretCredentialsEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessDelegationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DelegationPeriodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BalanceResponsibilityRequestEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BalanceResponsibilityRelationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CutoffEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationDomainEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorConsolidationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorConsolidationAuditLogEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdditionalRecipientEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdditionalRecipientOfMeteringPointEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    // How to do: https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/events
    // But it does not work: https://github.com/dotnet/EntityFramework.Docs/issues/3267
    // But there is a workaround: https://github.com/dotnet/EntityFramework.Docs/issues/3888
    private void OnEntityStateChanged(EntityEntry entityEntry)
    {
        if (entityEntry.Entity is IAuditedEntity changedByIdentity)
        {
            switch (entityEntry.State)
            {
                case EntityState.Modified:
                case EntityState.Added:
                    entityEntry.Property(nameof(IAuditedEntity.Version)).CurrentValue = changedByIdentity.Version + 1;
                    entityEntry.Property(nameof(IAuditedEntity.ChangedByIdentityId)).CurrentValue = _auditIdentityProvider.IdentityId.Value;
                    break;
            }
        }

        if (entityEntry is { Entity: IDeletableAuditedEntity deletedAuditedEntity, State: EntityState.Deleted })
        {
            PatchDeletedBy((dynamic)deletedAuditedEntity);
        }
    }

    private void PatchDeletedBy<T>(T entityDeleted)
        where T : class, IDeletableAuditedEntity
    {
        if (Database.CurrentTransaction == null)
        {
            if (!_savingChanges)
                throw new InvalidOperationException("Deleting audited entity requires a transaction. Since the audited entity was deleted outside of SaveChanges, a transaction is not started automatically.");

            Database.BeginTransaction();
        }

        Set<T>()
            .Where(entity => entity == entityDeleted)
            .ExecuteUpdate(props =>
                props.SetProperty(prop => prop.DeletedByIdentityId, _auditIdentityProvider.IdentityId.Value));
    }
}
