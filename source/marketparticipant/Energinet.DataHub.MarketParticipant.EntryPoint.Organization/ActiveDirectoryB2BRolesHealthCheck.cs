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
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization;

public sealed class ActiveDirectoryB2BRolesHealthCheck : IHealthCheck
{
    private readonly IActiveDirectoryB2BRolesProvider _activeDirectoryB2BRolesProvider;

    public ActiveDirectoryB2BRolesHealthCheck(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        _activeDirectoryB2BRolesProvider = container.GetInstance<IActiveDirectoryB2BRolesProvider>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var roles = await _activeDirectoryB2BRolesProvider
            .GetB2BRolesAsync()
            .ConfigureAwait(false);

        return roles.EicRolesMapped.Any()
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Active Directory B2B Roles are missing.");
    }
}
