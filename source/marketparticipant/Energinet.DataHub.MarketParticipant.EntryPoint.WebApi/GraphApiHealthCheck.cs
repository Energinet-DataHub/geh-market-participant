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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public sealed class GraphApiHealthCheck : IHealthCheck
{
    private readonly IUserIdentityRepository _userIdentityRepository;

    public GraphApiHealthCheck(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        _userIdentityRepository = container.GetInstance<IUserIdentityRepository>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Check that it is possible to connect to the Graph API and see that a user does not exist.
        await _userIdentityRepository
           .GetAsync(new ExternalUserId(Guid.Empty))
           .ConfigureAwait(false);

        return HealthCheckResult.Healthy();
    }
}
