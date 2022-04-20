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
using System.Threading.Tasks;
using Azure.Identity;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Common.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests
{
    public sealed class IntegrationTestHost : IAsyncDisposable
    {
        private readonly Startup _startup;

        private IntegrationTestHost()
        {
            _startup = new Startup();
        }

        public static async Task<IntegrationTestHost> InitializeAsync()
        {
            var integrationTestConfig = new IntegrationTestConfiguration();
            var host = new IntegrationTestHost();

            var serviceCollection = new ServiceCollection();
            host._startup.ConfigureServices(serviceCollection);
            serviceCollection.BuildServiceProvider().UseSimpleInjector(
                host._startup.Container,
                o => o.Container.Options.EnableAutoVerification = false);
            host._startup.Container.Options.AllowOverridingRegistrations = true;
            host._startup.Container.Register(
                () =>
                {
                    var clientSecretCredential = new ClientSecretCredential(
                        integrationTestConfig.B2CSettings.Tenant,
                        integrationTestConfig.B2CSettings.ServicePrincipalId,
                        integrationTestConfig.B2CSettings.ServicePrincipalSecret);

                    return new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
                },
                Lifestyle.Scoped);
            host._startup.Container.RegisterSingleton(() => new AzureAdConfig(
                integrationTestConfig.B2CSettings.BackendServicePrincipalObjectId,
                integrationTestConfig.B2CSettings.BackendAppId));
            host._startup.Container.AddActiveDirectoryRoles();

            return host;
        }

        public Scope BeginScope()
        {
            return AsyncScopedLifestyle.BeginScope(_startup.Container);
        }

        public async ValueTask DisposeAsync()
        {
            await _startup.DisposeAsync().ConfigureAwait(false);
        }
    }
}
