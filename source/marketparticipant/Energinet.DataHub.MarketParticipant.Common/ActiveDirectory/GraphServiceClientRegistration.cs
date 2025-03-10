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

using Azure.Identity;
using Energinet.DataHub.MarketParticipant.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Common.ActiveDirectory;

internal static class GraphServiceClientRegistration
{
    public static void AddGraphServiceClient(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureB2COptions>>();

            var clientSecretCredential = new ClientSecretCredential(
                options.Value.Tenant,
                options.Value.SpnId,
                options.Value.SpnSecret);

            return new GraphServiceClient(clientSecretCredential, ["https://graph.microsoft.com/.default"]);
        });
    }
}
