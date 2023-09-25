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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public sealed class SigningKeyRingHealthCheck : IHealthCheck
{
    private readonly ISigningKeyRing _signingKeyRing;

    public SigningKeyRingHealthCheck(ISigningKeyRing signingKeyRing)
    {
        _signingKeyRing = signingKeyRing;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var roles = await _signingKeyRing
            .GetKeysAsync()
            .ConfigureAwait(false);

        return roles.Any()
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Token signing keys are missing.");
    }
}
