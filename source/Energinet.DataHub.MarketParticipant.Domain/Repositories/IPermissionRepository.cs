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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetForMarketRoleAsync(EicFunction eicFunction);
    Task<IEnumerable<EicFunction>> GetAssignedToMarketRolesAsync(PermissionId permission);

    Task<Permission> GetAsync(PermissionId permission);
    Task<IEnumerable<Permission>> GetAsync(IEnumerable<PermissionId> permissions);

    Task UpdatePermissionAsync(Permission permission);
}
