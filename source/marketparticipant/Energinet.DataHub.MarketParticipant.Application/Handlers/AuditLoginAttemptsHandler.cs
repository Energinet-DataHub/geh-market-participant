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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.RevisionLog.Integration;
using MediatR;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers;

public sealed class AuditLoginAttemptsHandler : IRequestHandler<AuditLoginAttemptsCommand>
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    private readonly IB2CLogRepository _b2CLogRepository;
    private readonly ICutoffRepository _cutoffRepository;
    private readonly IRevisionLogClient _revisionLogClient;
    private readonly IUserRepository _userRepository;

    public AuditLoginAttemptsHandler(IB2CLogRepository b2CLogRepository, ICutoffRepository cutoffRepository, IRevisionLogClient revisionLogClient, IUserRepository userRepository)
    {
        _b2CLogRepository = b2CLogRepository;
        _cutoffRepository = cutoffRepository;
        _revisionLogClient = revisionLogClient;
        _userRepository = userRepository;
    }

    public async Task Handle(AuditLoginAttemptsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cutoff = await _cutoffRepository.GetCutoffAsync(CutoffType.B2CLoginAttempt).ConfigureAwait(false);

        await foreach (var logEntry in _b2CLogRepository.GetLoginAttempsAsync(cutoff).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var user = await _userRepository.GetAsync(new ExternalUserId(logEntry.UserId)).ConfigureAwait(false);
            var revisionLogEntry = CreateRevisionLogEntry(logEntry, user);

            await _revisionLogClient.LogAsync(
                    revisionLogEntry)
                .ConfigureAwait(false);

            if (cutoff < logEntry.AttemptedAt)
            {
                cutoff = logEntry.AttemptedAt;
                await _cutoffRepository.UpdateCutoffAsync(CutoffType.B2CLoginAttempt, cutoff).ConfigureAwait(false);
            }
        }
    }

    private static RevisionLogEntry CreateRevisionLogEntry(B2CLoginAttemptLogEntry logEntry, User? user)
    {
        return new RevisionLogEntry(
            logId: Guid.Parse(logEntry.Id),
            systemId: SubsystemInformation.Id,
            userId: user?.Id.Value ?? default,
            activity: "LoginAttempt",
            occurredOn: logEntry.AttemptedAt,
            origin: "B2C Login Audit Log",
            payload: JsonSerializer.Serialize(logEntry, _jsonSerializerOptions));
    }
}
