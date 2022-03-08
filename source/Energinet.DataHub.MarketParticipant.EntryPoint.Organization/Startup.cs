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

using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.HealthCheck;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization
{
    internal sealed class Startup : StartupBase
    {
        protected override void Configure(Container container)
        {
            Container.Register<CreateOrganizationFunction>();
            Container.Register<CreateActorFunction>();
            Container.Register<HealthFunction>();

            // health check
            container.Register<ISqlDatabaseVerifier, SqlDatabaseVerifier>(Lifestyle.Scoped);
            container.Register<IServiceBusQueueVerifier, ServiceBusQueueVerifier>(Lifestyle.Scoped);
            container.Register<IHealth, Health>(Lifestyle.Scoped);
        }
    }
}
