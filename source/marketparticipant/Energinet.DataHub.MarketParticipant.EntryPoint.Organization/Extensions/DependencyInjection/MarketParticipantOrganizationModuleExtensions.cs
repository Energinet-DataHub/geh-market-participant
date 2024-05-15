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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Logging.LoggingScopeMiddleware;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Energinet.DataHub.MarketParticipant.Application.Contracts;
using Energinet.DataHub.MarketParticipant.Application.Options;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Configuration;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Integration;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Monitor;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using SendGrid.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions.DependencyInjection;

internal static class MarketParticipantOrganizationModuleExtensions
{
    public static IServiceCollection AddMarketParticipantOrganizationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarketParticipantCore();

        services.AddScoped<IAuditIdentityProvider>(_ => KnownAuditIdentityProvider.OrganizationBackgroundService);
        services.AddFeatureManagement();

        services.AddOptions();
        services.Configure<ConsumeServiceBusSettings>(configuration.GetSection(nameof(ConsumeServiceBusSettings)));
        services.AddOptions<SendGridOptions>().BindConfiguration(SendGridOptions.SectionName).ValidateDataAnnotations();

        services.AddScoped<SynchronizeActorsTimerTrigger>();
        services.AddScoped<EmailEventTimerTrigger>();
        services.AddScoped<UserInvitationExpiredTimerTrigger>();
        services.AddScoped<DispatchIntegrationEventsTrigger>();
        services.AddScoped<ReceiveIntegrationEventsTrigger>();
        services.AddScoped<OrganizationIdentityUpdateTrigger>();

        services.AddPublisher<IntegrationEventProvider>();
        services.Configure<PublisherOptions>(options =>
        {
            options.ServiceBusConnectionString = configuration.GetSetting(Settings.ServiceBusTopicConnectionString);
            options.TopicName = configuration.GetSetting(Settings.ServiceBusTopicName);
        });

        services.AddSubscriber<IntegrationEventSubscriptionHandler>(new[]
        {
            BalanceResponsiblePartiesChanged.Descriptor,
        });

        services.AddSendGrid((provider, options) =>
        {
            var sendGridOptions = provider.GetRequiredService<IOptions<SendGridOptions>>();
            options.ApiKey = sendGridOptions.Value.ApiKey;
        });

        services.AddFunctionLoggingScope("mark-part");

        AddHealthChecks(configuration, services);

        return services;
    }

    private static void AddHealthChecks(IConfiguration configuration, IServiceCollection services)
    {
        static async Task<bool> CheckExpiredEventsAsync(MarketParticipantDbContext context, CancellationToken cancellationToken)
        {
            var healthCutoff = DateTimeOffset.UtcNow.AddDays(-1);

            var expiredDomainEvents = await context.DomainEvents
                .AnyAsync(e => !e.IsSent && e.Timestamp < healthCutoff, cancellationToken)
                .ConfigureAwait(false);

            return !expiredDomainEvents;
        }

        static async Task<bool> CheckExpiredEmailsAsync(MarketParticipantDbContext context, CancellationToken cancellationToken)
        {
            var healthCutoff = DateTimeOffset.UtcNow.AddDays(-1);

            var expiredEmails = await context.EmailEventEntries
                .AnyAsync(e => e.Sent == null && e.Created < healthCutoff, cancellationToken)
                .ConfigureAwait(false);

            return !expiredEmails;
        }

        var consumeEventsOptions = configuration.GetSection(nameof(ConsumeServiceBusSettings)).Get<ConsumeServiceBusSettings>()!;

        services.AddScoped<HealthCheckEndpoint>();

        services
            .AddHealthChecks()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddDbContextCheck<MarketParticipantDbContext>(customTestQuery: CheckExpiredEventsAsync, name: "expired_events")
            .AddDbContextCheck<MarketParticipantDbContext>(customTestQuery: CheckExpiredEmailsAsync, name: "expired_emails")
            .AddAzureServiceBusSubscription(
                _ => configuration.GetSetting(Settings.ServiceBusHealthConnectionString),
                _ => configuration.GetSetting(Settings.ServiceBusTopicName),
                _ => consumeEventsOptions.IntegrationEventSubscription) // This is the name of the subscription in the infrastructure project
            .AddAzureServiceBusSubscription(
                _ => configuration.GetSetting(Settings.ServiceBusHealthConnectionString),
                _ => consumeEventsOptions.SharedIntegrationEventTopic,
                _ => consumeEventsOptions.IntegrationEventSubscription,
                name: "integration event consumer")
            .AddSendGrid()
            .AddCheck<ActiveDirectoryB2BRolesHealthCheck>("AD B2B Roles Check");
    }
}
