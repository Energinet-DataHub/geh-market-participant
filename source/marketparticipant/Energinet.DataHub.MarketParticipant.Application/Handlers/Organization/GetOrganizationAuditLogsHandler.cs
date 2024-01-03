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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;

public sealed class GetOrganizationAuditLogsHandler
    : IRequestHandler<GetOrganizationAuditLogsCommand, GetOrganizationAuditLogsResponse>
{
    private readonly IOrganizationAuditLogRepository _organizationAuditLogRepository;

    public GetOrganizationAuditLogsHandler(IOrganizationAuditLogRepository organizationAuditLogRepository)
    {
        _organizationAuditLogRepository = organizationAuditLogRepository;
    }

    public async Task<GetOrganizationAuditLogsResponse> Handle(
        GetOrganizationAuditLogsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _organizationAuditLogRepository
            .GetAsync(new OrganizationId(request.OrganizationId))
            .ConfigureAwait(false);

        return new GetOrganizationAuditLogsResponse(auditLogs
            .OrderBy(log => log.Timestamp)
            .Select(log => new AuditLogDto<OrganizationAuditedChange>(log)));
    }
}
