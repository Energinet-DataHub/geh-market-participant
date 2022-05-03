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

using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.Common.SimpleInjector;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.HealthCheck;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization
{
    internal sealed class Startup : StartupBase
    {
        protected override void Configure(IServiceCollection services)
        {
        }

        protected override void ConfigureSimpleInjector(IServiceCollection services)
        {
            var descriptor = new ServiceDescriptor(
                typeof(IFunctionActivator),
                typeof(SimpleInjectorActivator),
                ServiceLifetime.Singleton);

            services.Replace(descriptor);

            services.AddSimpleInjector(Container, x =>
            {
                x.DisposeContainerWithServiceProvider = false;
                x.AddLogging();
            });
        }

        protected override void Configure(Container container)
        {
            Container.Register<HealthFunction>();
            Container.Register<DispatchEventsTimerTrigger>();

            // health check
            container.Register<ISqlDatabaseVerifier, SqlDatabaseVerifier>(Lifestyle.Scoped);
            container.Register<IServiceBusQueueVerifier, ServiceBusQueueVerifier>(Lifestyle.Scoped);
            container.Register<IHealth, Health>(Lifestyle.Scoped);
        }
    }
}
