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
using System.Net.Http.Headers;
using System.Text;
using Energinet.DataHub.MarketParticipant.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.Common.ActiveDirectory;

internal static class CvrRegisterRegistration
{
    public static void AddCvrRegisterConfiguration(this IServiceCollection services)
    {
        services.AddOptions<CvrRegisterOptions>().BindConfiguration(CvrRegisterOptions.SectionName).ValidateDataAnnotations();

        services.AddHttpClient("CvrRegister", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CvrRegisterOptions>>();

            client.BaseAddress = options.Value.BaseAddress;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Value.Username}:{options.Value.Password}")));
        });
    }
}
