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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Extensions.DependencyInjection;

internal static class HttpClientExtensions
{
    public static IServiceCollection RegisterHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<HttpClient>((_, httpClient)
            => httpClient.BaseAddress = new Uri($"https://management.azure.com{configuration.GetValue<string>("APIM_SERVICE_NAME")}/certificates/"));
        return services;
    }
}
