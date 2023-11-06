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
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public sealed class CertificateKeyVaultHealthCheck : IHealthCheck
{
    private readonly SecretClient _secretClient;

    public CertificateKeyVaultHealthCheck(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var unused in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                break;
            }

            return HealthCheckResult.Healthy();
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy("Could not connect to key vault.");
        }
    }
}
