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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class ActorAuditLogRepository : IActorAuditLogRepository
{
    private readonly IMarketParticipantDbContext _context;

    public ActorAuditLogRepository(IMarketParticipantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetAsync(ActorId actor)
    {
        var actorAuditLogs = await GetActorAuditsAsync(actor)
            .ConfigureAwait(false);

        var certificateAuditLogs = await GetActorCertificateCredentialAuditsAsync(actor)
            .ConfigureAwait(false);

        var clientSecretAuditLogs = await GetActorClientSecretCredentialAuditsAsync(actor)
            .ConfigureAwait(false);

        return actorAuditLogs
            .Concat(certificateAuditLogs)
            .Concat(clientSecretAuditLogs);
    }

    private Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetActorAuditsAsync(ActorId actor)
    {
        var dataSource = new HistoryTableDataSource<ActorEntity>(_context.Actors, entity => entity.Id == actor.Value);

        return new AuditLogBuilder<ActorAuditedChange, ActorEntity>(dataSource)
            .Add(ActorAuditedChange.Name, entity => entity.Name, AuditedChangeCompareAt.Creation)
            .Add(ActorAuditedChange.Status, entity => entity.Status, AuditedChangeCompareAt.Creation)
            .BuildAsync();
    }

    private Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetActorCertificateCredentialAuditsAsync(ActorId actor)
    {
        var dataSource = new HistoryTableDataSource<ActorCertificateCredentialsEntity>(_context.ActorCertificateCredentials, entity => entity.ActorId == actor.Value);

        return new AuditLogBuilder<ActorAuditedChange, ActorCertificateCredentialsEntity>(dataSource)
            .Add(ActorAuditedChange.CertificateCredentials, entity => entity.CertificateThumbprint, AuditedChangeCompareAt.BothCreationAndDeletion)
            .WithGrouping(entity => entity.Id)
            .BuildAsync();
    }

    private Task<IEnumerable<AuditLog<ActorAuditedChange>>> GetActorClientSecretCredentialAuditsAsync(ActorId actor)
    {
        var dataSource = new HistoryTableDataSource<ActorClientSecretCredentialsEntity>(_context.ActorClientSecretCredentials, entity => entity.ActorId == actor.Value);

        return new AuditLogBuilder<ActorAuditedChange, ActorClientSecretCredentialsEntity>(dataSource)
            .Add(ActorAuditedChange.ClientSecretCredentials, entity => entity.ExpirationDate.ToInstant(), AuditedChangeCompareAt.BothCreationAndDeletion)
            .WithGrouping(entity => entity.Id)
            .BuildAsync();
    }
}
