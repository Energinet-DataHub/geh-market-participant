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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorAuditLogEntryRepository : IActorAuditLogEntryRepository
{
    private readonly IMarketParticipantDbContext _context;

    public ActorAuditLogEntryRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ActorAuditLogEntry>> GetAsync(ActorId actor)
    {
        var actorAuditLogs = await GetActorAuditsAsync(actor)
            .ConfigureAwait(false);

        var certificateAuditLogs = await GetActorCertificateCredentialAuditsAsync(actor)
            .ConfigureAwait(false);

        var clientSecretAuditLogs = await GetActorClientSecretCredentialAuditsAsync(actor)
            .ConfigureAwait(false);

        return actorAuditLogs
            .Concat(certificateAuditLogs)
            .Concat(clientSecretAuditLogs)
            .OrderBy(entry => entry.Timestamp)
            .ToList();
    }

    private async Task<IEnumerable<ActorAuditLogEntry>> GetActorAuditsAsync(ActorId actor)
    {
        var historicEntities = await _context.Actors
            .ReadAllHistoryForAsync(entity => entity.Id == actor.Value)
            .ConfigureAwait(false);

        var auditedProperties = new[]
        {
            new
            {
                Property = ActorChangeType.Name,
                ReadValue = new Func<ActorEntity, object?>(entity => entity.Name)
            },
            new
            {
                Property = ActorChangeType.Status,
                ReadValue = new Func<ActorEntity, object?>(entity => entity.Status)
            },
        };

        var auditEntries = new List<ActorAuditLogEntry>();

        for (var i = 0; i < historicEntities.Count; i++)
        {
            var isFirst = i == 0;
            var current = historicEntities[i];
            var previous = isFirst ? current : historicEntities[i - 1];

            if (isFirst)
            {
                auditEntries.Add(new ActorAuditLogEntry(
                    actor,
                    new AuditIdentity(current.Entity.ChangedByIdentityId),
                    ActorChangeType.Created,
                    current.PeriodStart,
                    string.Empty,
                    string.Empty));
            }

            foreach (var auditedProperty in auditedProperties)
            {
                var currentValue = auditedProperty.ReadValue(current.Entity);
                var previousValue = auditedProperty.ReadValue(previous.Entity);

                if (!Equals(currentValue, previousValue))
                {
                    auditEntries.Add(new ActorAuditLogEntry(
                        actor,
                        new AuditIdentity(current.Entity.ChangedByIdentityId),
                        auditedProperty.Property,
                        current.PeriodStart,
                        currentValue?.ToString() ?? string.Empty,
                        previousValue?.ToString() ?? string.Empty));
                }
            }
        }

        return auditEntries;
    }

    private async Task<IEnumerable<ActorAuditLogEntry>> GetActorCertificateCredentialAuditsAsync(ActorId actor)
    {
        var historicEntities = await _context.ActorCertificateCredentials
            .ReadAllHistoryForAsync(entity => entity.ActorId == actor.Value)
            .ConfigureAwait(false);

        var auditEntries = new List<ActorAuditLogEntry>();

        foreach (var (entity, periodStart) in historicEntities)
        {
            if (entity.DeletedByIdentityId == null)
            {
                auditEntries.Add(new ActorAuditLogEntry(
                    actor,
                    new AuditIdentity(entity.ChangedByIdentityId),
                    ActorChangeType.CertificateCredentials,
                    periodStart,
                    entity.CertificateThumbprint,
                    string.Empty));
            }
            else
            {
                auditEntries.Add(new ActorAuditLogEntry(
                    actor,
                    new AuditIdentity(entity.ChangedByIdentityId),
                    ActorChangeType.CertificateCredentials,
                    periodStart,
                    string.Empty,
                    entity.CertificateThumbprint));
            }
        }

        return auditEntries;
    }

    private async Task<IEnumerable<ActorAuditLogEntry>> GetActorClientSecretCredentialAuditsAsync(ActorId actor)
    {
        var historicEntities = await _context.ActorClientSecretCredentials
            .ReadAllHistoryForAsync(entity => entity.ActorId == actor.Value)
            .ConfigureAwait(false);

        var auditEntries = new List<ActorAuditLogEntry>();

        foreach (var (entity, periodStart) in historicEntities)
        {
            if (entity.DeletedByIdentityId == null)
            {
                auditEntries.Add(new ActorAuditLogEntry(
                    actor,
                    new AuditIdentity(entity.ChangedByIdentityId),
                    ActorChangeType.SecretCredentials,
                    periodStart,
                    entity
                        .ExpirationDate
                        .ToInstant()
                        .ToString("g", CultureInfo.InvariantCulture),
                    string.Empty));
            }
            else
            {
                auditEntries.Add(new ActorAuditLogEntry(
                    actor,
                    new AuditIdentity(entity.ChangedByIdentityId),
                    ActorChangeType.SecretCredentials,
                    periodStart,
                    string.Empty,
                    entity
                        .ExpirationDate
                        .ToInstant()
                        .ToString("g", CultureInfo.InvariantCulture)));
            }
        }

        return auditEntries;
    }
}
