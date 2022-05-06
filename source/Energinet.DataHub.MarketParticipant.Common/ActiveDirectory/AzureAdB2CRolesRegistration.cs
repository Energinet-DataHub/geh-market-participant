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
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.Common.ActiveDirectory
{
    internal static class AzureAdB2CRolesRegistration
    {
        public static void AddActiveDirectoryRoles(this Container container)
        {
            container.RegisterSingleton(() =>
            {
                var configuration = container.GetService<IConfiguration>();
                var graphClient = container.GetInstance<GraphServiceClient>();
                var appObjectId = configuration!["AZURE_B2C_BACKEND_OBJECT_ID"];

                if (string.IsNullOrWhiteSpace(appObjectId))
                {
                    throw new InvalidOperationException(
                        "Key 'AZURE_B2C_BACKEND_OBJECT_ID' is null, empty or whitespace");
                }

                return new ActiveDirectoryB2CRolesProvider(graphClient, appObjectId);
            });
        }
    }
}
