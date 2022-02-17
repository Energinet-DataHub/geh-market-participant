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
using Energinet.DataHub.MarketParticipant.Application;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common.MediatR;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization
{
    internal sealed class Startup : StartupBase
    {
        protected override void Configure(Container container)
        {
            container.Register<IOrganizationRepository, OrganizationRepository>(Lifestyle.Singleton);

            // Services
            Container.AddApplicationServices();
            Container.Register<IOrganizationChangedEventParser, OrganizationChangedEventParser>(Lifestyle.Transient);

            // Functions
            Container.Register<CreateOrganizationFunction>();

            // ServiceBus config
            AddServiceBusConfig(container);

            // Add MediatR
            Container.BuildMediator(new[] { typeof(ApplicationAssemblyReference).Assembly });
        }

        private static void AddServiceBusConfig(Container container)
        {
            container.RegisterSingleton(() =>
            {
                var configuration = container.GetService<IConfiguration>();

                return new ServiceBusConfig(
                    configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING"),
                    configuration.GetValue<string>("SBT_MARKET_PARTICIPANT_CHANGED_NAME"));
            });
        }
    }
}
