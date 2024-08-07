﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public sealed class RevisionLogHealthCheck : IHealthCheck
{
    private readonly IOptions<RevisionLogOptions> _revisionLogOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public RevisionLogHealthCheck(
        IOptions<RevisionLogOptions> revisionLogOptions,
        IHttpClientFactory httpClientFactory)
    {
        _revisionLogOptions = revisionLogOptions;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthAddress = new UriBuilder(_revisionLogOptions.Value.ApiAddress)
        {
            Path = "api/monitor/live"
        };

        using var logRequest = new HttpRequestMessage(HttpMethod.Get, healthAddress.Uri);

        using var httpClient = _httpClientFactory.CreateClient("revision-log-http-client");
        using var response = await httpClient
            .SendAsync(logRequest, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return HealthCheckResult.Healthy();
    }
}
