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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;

public sealed class GetPermissionRelationsHandler
    : IRequestHandler<GetPermissionRelationsCommand, Stream>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetPermissionRelationsHandler(
        IPermissionRepository permissionRepository,
        IUserRoleRepository userRoleRepository)
    {
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Stream> Handle(GetPermissionRelationsCommand request, CancellationToken cancellationToken)
    {
        var allPermissions = await _permissionRepository.GetAllAsync().ConfigureAwait(false);

        var allUserRoles = (await _userRoleRepository.GetAllAsync().ConfigureAwait(false)).ToList();

        var allMarketRoles = Enum.GetNames<EicFunction>();

        var records = new List<RelationRecord>();

        foreach (var permission in allPermissions)
        {
            var userRoles = allUserRoles.Where(x => x.Permissions.Contains(permission.Id)).ToList();

            if (userRoles.Any())
            {
                foreach (var userRole in userRoles)
                {
                    records.Add(new RelationRecord(permission.Name, userRole.EicFunction.ToString(), userRole.Name));
                }
            }
            else
            {
                records.Add(new RelationRecord(permission.Name, string.Empty, string.Empty));
            }
        }

        foreach (var marketRole in allMarketRoles)
        {
            if (records.All(x => x.MarketRole != marketRole))
            {
                records.Add(new RelationRecord(string.Empty, marketRole, string.Empty));
            }
        }

        return WriteRecordsToStream(records.OrderBy(e => e.MarketRole).ThenBy(e => e.Permission));
    }

    private static Stream WriteRecordsToStream(IEnumerable<RelationRecord> records)
    {
        using var stringWriter = new StringWriter();
        stringWriter.WriteLine("PermissionName;MarketRole;UserRole");

        foreach (var record in records)
        {
            stringWriter.WriteLine($"{record.Permission};{record.MarketRole};{record.UserRole}");
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(stringWriter.ToString()));
        return stream;
    }

    private sealed record RelationRecord(string Permission, string MarketRole, string UserRole);
}
