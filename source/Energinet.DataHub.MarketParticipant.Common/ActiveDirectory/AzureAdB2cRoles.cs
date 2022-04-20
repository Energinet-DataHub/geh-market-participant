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
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.Common.ActiveDirectory
{
    internal static class AzureAdB2cRoles
    {
        public static void GetActiveDirectoryRoles(this Container container)
        {
            container.RegisterSingleton(async () =>
            {
                var configuration = container.GetService<IConfiguration>();
                var graphClient = container.GetInstance<GraphServiceClient>();
                var appObjectId = configuration!["AZURE_B2C_BACKEND_OBJECT_ID"];
                var activeDirectoryB2CRoles = new ActiveDirectoryB2CRoles();

                var application = await graphClient.Applications[appObjectId]
                    .Request()
                    .Select(a => new { a.DisplayName, a.AppRoles })
                    .GetAsync()
                    .ConfigureAwait(false);

                if (application is null)
                {
                    throw new InvalidOperationException(
                        $"No application, '{nameof(application)}', was found in Active Directory.");
                }

                foreach (var appRole in application.AppRoles)
                {
                    switch (appRole.DisplayName)
                    {
                        case "Balance Responsible Party":
                            activeDirectoryB2CRoles.DdkId = appRole.Id ?? Guid.Empty;
                            break;
                        case "Grid operator":
                            activeDirectoryB2CRoles.DdmId = appRole.Id ?? Guid.Empty;
                            break;
                        case "Electrical supplier":
                            activeDirectoryB2CRoles.DdqId = appRole.Id ?? Guid.Empty;
                            break;
                        case "Transmission system operator":
                            activeDirectoryB2CRoles.EzId = appRole.Id ?? Guid.Empty;
                            break;
                        case "Meter data responsible":
                            activeDirectoryB2CRoles.MdrId = appRole.Id ?? Guid.Empty;
                            break;
                        case "STS":
                            activeDirectoryB2CRoles.StsId = appRole.Id ?? Guid.Empty;
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Could not find an id associated with the provided role name '{appRole.DisplayName}'.");
                    }
                }

                return activeDirectoryB2CRoles;
            });
        }
    }
}
