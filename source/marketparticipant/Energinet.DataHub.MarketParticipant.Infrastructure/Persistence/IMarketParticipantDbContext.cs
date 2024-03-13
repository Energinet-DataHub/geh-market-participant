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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;

/// <summary>
///     The interface used for the DB context for the MarketParticipant database
/// </summary>
public interface IMarketParticipantDbContext
{
    /// <summary>
    ///     Represent access to the organization database table
    /// </summary>
    DbSet<OrganizationEntity> Organizations { get; }

    /// <summary>
    ///     Represent access to the actor database table
    /// </summary>
    DbSet<ActorEntity> Actors { get; }

    /// <summary>
    ///     Represent access to the GridAreas database table
    /// </summary>
    DbSet<GridAreaEntity> GridAreas { get; }

    /// <summary>
    ///     Represent access to the MarketRole database table
    /// </summary>
    DbSet<MarketRoleEntity> MarketRoles { get; }

    /// <summary>
    ///     Represent access to the MarketRoleGridArea database table
    /// </summary>
    DbSet<MarketRoleGridAreaEntity> MarketRoleGridAreas { get; }

    /// <summary>
    ///     Represent access to the ActorContacts database table
    /// </summary>
    DbSet<ActorContactEntity> ActorContacts { get; }

    /// <summary>
    ///     Represent access to the GridAreas database table
    /// </summary>
    DbSet<GridAreaLinkEntity> GridAreaLinks { get; }

    /// <summary>
    ///     Represent access to the UniqueActorMarketRoleGridArea database table
    /// </summary>
    DbSet<UniqueActorMarketRoleGridAreaEntity> UniqueActorMarketRoleGridAreas { get; }

    /// <summary>
    ///     Represent access to the User database table
    /// </summary>
    DbSet<UserEntity> Users { get; }

    /// <summary>
    ///     Represent access to the User role Assignments database table
    /// </summary>
    DbSet<UserRoleAssignmentEntity> UserRoleAssignments { get; }

    /// <summary>
    ///     Represent access to the UserRoles database table
    /// </summary>
    DbSet<UserRoleEntity> UserRoles { get; }

    /// <summary>
    ///     Represent access to the UserRoles permissions relation database table
    /// </summary>
    DbSet<UserRolePermissionEntity> UserRolePermissionEntries { get; }

    /// <summary>
    ///     Represent access to the UserRoleAssignmentAuditLogEntry database table
    /// </summary>
    DbSet<UserRoleAssignmentAuditLogEntryEntity> UserRoleAssignmentAuditLogEntries { get; }

    /// <summary>
    ///     Represent access to the UserInviteAuditLogEntry database table
    /// </summary>
    DbSet<UserInviteAuditLogEntryEntity> UserInviteAuditLogEntries { get; }

    /// <summary>
    ///     Represent access to the UserIdentityAuditLogEntry database table
    /// </summary>
    DbSet<UserIdentityAuditLogEntryEntity> UserIdentityAuditLogEntries { get; }

    /// <summary>
    ///     Represent access to the Permission database table
    /// </summary>
    DbSet<PermissionEntity> Permissions { get; }

    /// <summary>
    ///     Represent access to the DomainEvents database table
    /// </summary>
    DbSet<DomainEventEntity> DomainEvents { get; }

    /// <summary>
    ///     Represent access to the EmailEventEntry database table
    /// </summary>
    DbSet<EmailEventEntity> EmailEventEntries { get; }

    /// <summary>
    ///     Represent access to the ActorCertificateCredentials database table
    /// </summary>
    DbSet<ActorCertificateCredentialsEntity> ActorCertificateCredentials { get; }

    /// <summary>
    ///     Represent access to the ActorClientSecretCredentials database table
    /// </summary>
    DbSet<ActorClientSecretCredentialsEntity> ActorClientSecretCredentials { get; }

    /// <summary>
    ///     Represent access to the UsedActorCertificates database table
    /// </summary>
    DbSet<UsedActorCertificatesEntity> UsedActorCertificates { get; }

    /// <summary>
    ///     Represent access to the MessageDelegation database table
    /// </summary>
    DbSet<MessageDelegationEntity> MessageDelegations { get; }

    /// <summary>
    ///     Represent access to the DelegationPeriod database table
    /// </summary>
    DbSet<DelegationPeriodEntity> DelegationPeriods { get; }

    /// <summary>
    ///     Saves changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    ///     Gets the EntityEntry for the given Entry
    /// </summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task CreateLockAsync(LockableEntity lockableEntity);
}
