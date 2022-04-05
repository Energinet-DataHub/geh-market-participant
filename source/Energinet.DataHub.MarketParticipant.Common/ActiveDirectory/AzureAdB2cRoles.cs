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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.ActiveDirectory;
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
                var appObjectId = configuration.GetValue<string>("BackendAppObjectId");

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
                    if (string.Equals(appRole.DisplayName, "DDK", StringComparison.Ordinal))
                    {
                        ActiveDirectoryB2CRoles.DdkId = appRole.Id.ToString()!;
                    }
                    else if (string.Equals(appRole.DisplayName, "DDM", StringComparison.Ordinal))
                    {
                        ActiveDirectoryB2CRoles.DdmId = appRole.Id.ToString()!;
                    }
                    else if (string.Equals(appRole.DisplayName, "DDQ", StringComparison.Ordinal))
                    {
                        ActiveDirectoryB2CRoles.DdqId = appRole.Id.ToString()!;
                    }
                    else if (string.Equals(appRole.DisplayName, "EZ", StringComparison.Ordinal))
                    {
                        ActiveDirectoryB2CRoles.EzId = appRole.Id.ToString()!;
                    }
                    else if (string.Equals(appRole.DisplayName, "MDR", StringComparison.Ordinal))
                    {
                        ActiveDirectoryB2CRoles.MdrId = appRole.Id.ToString()!;
                    }
                    else if (string.Equals(appRole.DisplayName, "STS", StringComparison.Ordinal))
                    {
                        ActiveDirectoryB2CRoles.StsId = appRole.Id.ToString()!;
                    }
                }
            });
        }
    }
}
