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

using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.FunctionApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Email;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Monitor;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid.Extensions.DependencyInjection;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization;

internal sealed class Startup : StartupBase
{
    protected override void Configure(IConfiguration configuration, IServiceCollection services)
    {
        // TODO: These registrations should be removed when SimpleInjector is removed.
        services.AddScoped<IMarketParticipantDbContext, MarketParticipantDbContext>();
        services.AddScoped<IGridAreaRepository, GridAreaRepository>();
        services.AddScoped<IIntegrationEventFactory, IntegrationEventFactory>();

        services.AddPublisher<IntegrationEventProvider>();
        services.Configure<PublisherOptions>(options =>
        {
            options.ServiceBusConnectionString = configuration.GetSetting(Settings.ServiceBusTopicConnectionString);
            options.TopicName = configuration.GetSetting(Settings.ServiceBusTopicName);
        });

        services.AddSendGrid(options =>
        {
            options.ApiKey = configuration.GetOptionalSetting(Settings.SendGridApiKey);
        });

        // Health check
        services
            .AddHealthChecks()
            .AddLiveCheck()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddAzureServiceBusTopic(
                _ => configuration.GetSetting(Settings.ServiceBusHealthConnectionString),
                _ => configuration.GetSetting(Settings.ServiceBusTopicName));
    }

    protected override void Configure(IConfiguration configuration, Container container)
    {
        Container.Options.EnableAutoVerification = false;

        Container.Register<SynchronizeActorsTimerTrigger>();
        Container.Register<EmailEventTimerTrigger>();
        Container.Register<UserInvitationExpiredTimerTrigger>();
        Container.Register<DispatchIntegrationEventsTrigger>();
        Container.AddInviteConfigRegistration();
        Container.AddSendGridEmailSenderClient();

        // Health check
        container.Register<IHealthCheckEndpointHandler, HealthCheckEndpointHandler>(Lifestyle.Scoped);
        container.Register<HealthCheckEndpoint>(Lifestyle.Scoped);
    }
}
