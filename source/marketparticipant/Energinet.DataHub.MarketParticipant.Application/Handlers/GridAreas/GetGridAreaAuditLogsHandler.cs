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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.GridAreas;

public sealed class GetGridAreaAuditLogsHandler : IRequestHandler<GetGridAreaAuditLogsCommand, GetGridAreaAuditLogsResponse>
{
    private readonly IGridAreaRepository _gridAreaRepository;
    private readonly IGridAreaAuditLogRepository _gridAreaAuditLogRepository;
    private readonly IActorConsolidationAuditLogRepository _actorConsolidationAuditLogRepository;

    public GetGridAreaAuditLogsHandler(
        IGridAreaRepository gridAreaRepository,
        IGridAreaAuditLogRepository gridAreaAuditLogRepository,
        IActorConsolidationAuditLogRepository actorConsolidationAuditLogRepository)
    {
        _gridAreaRepository = gridAreaRepository;
        _gridAreaAuditLogRepository = gridAreaAuditLogRepository;
        _actorConsolidationAuditLogRepository = actorConsolidationAuditLogRepository;
    }

    public async Task<GetGridAreaAuditLogsResponse> Handle(GetGridAreaAuditLogsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var gridAreaId = new GridAreaId(request.GridAreaId);
        var gridArea = await _gridAreaRepository
            .GetAsync(gridAreaId)
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(gridArea, request.GridAreaId);

        var consolidationAuditLogs = await _actorConsolidationAuditLogRepository
            .GetAsync(gridAreaId)
            .ConfigureAwait(false);

        var gridAreaAuditLogs = await _gridAreaAuditLogRepository
            .GetAsync(gridAreaId)
            .ConfigureAwait(false);

        return new GetGridAreaAuditLogsResponse(gridAreaAuditLogs
            .Concat(consolidationAuditLogs)
            .OrderBy(log => log.Timestamp)
            .Select(log => new AuditLogDto<GridAreaAuditedChange>(log)));
    }
}
