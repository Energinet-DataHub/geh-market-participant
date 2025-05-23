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

using Energinet.DataHub.MarketParticipant.Authorization.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Extensions.HealthChecks;
public static class DataApiHealthCheckExtensions
{
    private const string Name = "ElectricityMarketApiHealthCheck";

    public static IHealthChecksBuilder AddElectricityMarketDataApiHealthCheck(
        this IHealthChecksBuilder builder,
        string? name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new HealthCheckRegistration(
            name ?? Name,
            serviceProvider => new DataApiHealthCheck(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<ElectricityMarketClientOptions>>()),
            failureStatus,
            tags,
            timeout));
    }
}
