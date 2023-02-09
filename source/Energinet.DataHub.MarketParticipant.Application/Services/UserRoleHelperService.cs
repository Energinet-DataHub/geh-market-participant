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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public class EnsureUserRolePermissionsService : IEnsureUserRolePermissionsService
    {
        private readonly IPermissionRepository _permissionRepository;

        public EnsureUserRolePermissionsService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<bool> EnsurePermissionsSelectedAreValidForMarketRoleAsync(IEnumerable<Permission> permissions, EicFunction eicFunction)
        {
            var permissionsToEic = await _permissionRepository.GetToMarketRoleAsync(eicFunction).ConfigureAwait(false);
            var valid = permissions.All(x => permissionsToEic.Any(y => y.Permission == x));
            return valid;
        }
    }
}
