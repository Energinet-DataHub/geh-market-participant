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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;

public sealed class GetActorAuditLogsHandler
    : IRequestHandler<GetActorAuditLogsCommand, GetActorAuditLogsResponse>
{
    private readonly IActorAuditLogEntryRepository _actorAuditLogEntryRepository;
    private readonly IActorContactAuditLogEntryRepository _actorContactAuditLogEntryRepository;

    public GetActorAuditLogsHandler(
        IActorAuditLogEntryRepository actorAuditLogEntryRepository,
        IActorContactAuditLogEntryRepository actorContactAuditLogEntryRepository)
    {
        _actorAuditLogEntryRepository = actorAuditLogEntryRepository;
        _actorContactAuditLogEntryRepository = actorContactAuditLogEntryRepository;
    }

    public async Task<GetActorAuditLogsResponse> Handle(
        GetActorAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actorAuditLogs = await _actorAuditLogEntryRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        var actorContactAuditLogs = await _actorContactAuditLogEntryRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        return new GetActorAuditLogsResponse(actorAuditLogs.Select(Map), actorContactAuditLogs.Select(Map));
    }

    private static ActorAuditLogDto Map(ActorAuditLogEntry auditLogEntry)
    {
        return new ActorAuditLogDto(
            auditLogEntry.ActorId.Value,
            auditLogEntry.CurrentValue,
            auditLogEntry.PreviousValue,
            auditLogEntry.AuditIdentity.Value,
            auditLogEntry.Timestamp,
            auditLogEntry.ActorChangeType);
    }

    private static ActorContactAuditLogDto Map(ActorContactAuditLogEntry auditLogEntry)
    {
        return new ActorContactAuditLogDto(
            auditLogEntry.ActorId.Value,
            auditLogEntry.CurrentValue,
            auditLogEntry.PreviousValue,
            auditLogEntry.AuditIdentity.Value,
            auditLogEntry.Timestamp,
            auditLogEntry.ContactCategory ?? ContactCategory.Default,
            auditLogEntry.ActorContactChangeType);
    }
}
