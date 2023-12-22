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
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea;

public sealed class GetGridAreaAuditLogsHandler : IRequestHandler<GetGridAreaAuditLogsCommand, GetGridAreaAuditLogsResponse>
{
    private readonly IGridAreaAuditLogRepository _repository;

    public GetGridAreaAuditLogsHandler(IGridAreaAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetGridAreaAuditLogsResponse> Handle(GetGridAreaAuditLogsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _repository
            .GetAsync(new GridAreaId(request.GridAreaId))
            .ConfigureAwait(false);

        return new GetGridAreaAuditLogsResponse(auditLogs
            .OrderBy(log => log.Timestamp)
            .Select(log => new AuditLogDto<GridAreaAuditedChange>(log)));
    }
}
