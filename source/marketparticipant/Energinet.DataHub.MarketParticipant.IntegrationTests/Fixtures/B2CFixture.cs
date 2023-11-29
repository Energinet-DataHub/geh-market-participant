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

using System.Threading.Tasks;
using Azure.Identity;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.Graph;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures
{
#pragma warning disable CA1001
    public sealed class B2CFixture : IAsyncLifetime
#pragma warning restore CA1001
    {
        private GraphServiceClient? _graphClient;

        public IActiveDirectoryB2CService B2CService { get; private set; } = null!;

        public Task InitializeAsync()
        {
            var integrationTestConfig = new IntegrationTestConfiguration();

            // Graph Service Client
            var clientSecretCredential = new ClientSecretCredential(
                integrationTestConfig.B2CSettings.Tenant,
                integrationTestConfig.B2CSettings.ServicePrincipalId,
                integrationTestConfig.B2CSettings.ServicePrincipalSecret);

            _graphClient = new GraphServiceClient(
                clientSecretCredential,
                new[]
                {
                    "https://graph.microsoft.com/.default"
                });

            // Azure AD Config
            var config = new AzureAdConfig(
                integrationTestConfig.B2CSettings.BackendServicePrincipalObjectId,
                integrationTestConfig.B2CSettings.BackendAppId);

            // Active Directory Roles
            var activeDirectoryB2CRoles =
                new ActiveDirectoryB2BRolesProvider(_graphClient, integrationTestConfig.B2CSettings.BackendAppObjectId);

            B2CService = new ActiveDirectoryB2CService(
                _graphClient,
                config,
                activeDirectoryB2CRoles);

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _graphClient?.Dispose();
            return Task.CompletedTask;
        }
    }
}
