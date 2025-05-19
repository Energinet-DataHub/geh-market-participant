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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Extensions.HealthChecks;

public class DataApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ElectricityMarketClientOptions _options;

    public DataApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<ElectricityMarketClientOptions> electricityMarketOptions)
    {
        ArgumentNullException.ThrowIfNull(electricityMarketOptions);

        _httpClientFactory = httpClientFactory;
        _options = electricityMarketOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = CreateHttpClient();
            var url = new Uri("/api/monitor/live", UriKind.Relative);
            var response = await httpClient
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy("Electricity Market DataAPI health check failed", ex);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient("ElectricityMarketClient");
        httpClient.BaseAddress = _options.BaseUrl;
        return httpClient;
    }
}
