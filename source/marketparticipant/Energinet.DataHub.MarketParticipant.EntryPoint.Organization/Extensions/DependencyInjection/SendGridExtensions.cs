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

using System.Net.Http;
using Energinet.DataHub.MarketParticipant.Application.Options;
using HealthChecks.SendGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions.DependencyInjection;

internal static class SendGridExtensions
{
    public static IHealthChecksBuilder AddSendGrid(this IHealthChecksBuilder builder)
    {
        const string registrationName = "sendgrid";

        builder.Services.AddHttpClient(registrationName);

        return builder.Add(new HealthCheckRegistration(
            registrationName,
            sp => new SendGridHealthCheck(sp.GetRequiredService<IOptions<SendGridOptions>>().Value.ApiKey, sp.GetRequiredService<IHttpClientFactory>()),
            default,
            default,
            default));
    }
}
