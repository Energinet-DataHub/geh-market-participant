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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;

public sealed class UpdatePermissionHandler
    : IRequestHandler<UpdatePermissionCommand, Unit>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionAuditLogEntryRepository _permissionAuditLogEntryRepository;

    public UpdatePermissionHandler(
        IPermissionRepository permissionRepository,
        IPermissionAuditLogEntryRepository permissionAuditLogEntryRepository)
    {
        _permissionRepository = permissionRepository;
        _permissionAuditLogEntryRepository = permissionAuditLogEntryRepository;
    }

    public async Task<Unit> Handle(
        UpdatePermissionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var permissionToUpdate = await _permissionRepository.GetAsync((Permission)request.PermissionId).ConfigureAwait(false);

        if (permissionToUpdate == null)
        {
            throw new NotFoundValidationException($"Permission not found: {request.PermissionId}");
        }

        permissionToUpdate.Description = request.Description;

        await _permissionRepository.UpdatePermissionAsync(permissionToUpdate).ConfigureAwait(false);

        await _permissionAuditLogEntryRepository
            .InsertAuditLogEntryAsync(new PermissionAuditLogEntry(
                permissionToUpdate.Permission,
                new UserId(request.ChangedByUserId),
                PermissionChangeType.DescriptionChange,
                DateTimeOffset.UtcNow))
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
