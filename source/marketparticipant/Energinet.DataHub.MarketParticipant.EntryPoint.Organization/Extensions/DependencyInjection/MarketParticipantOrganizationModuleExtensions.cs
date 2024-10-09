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
using Azure.Identity;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Builder;
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Energinet.DataHub.MarketParticipant.Application.Contracts;
using Energinet.DataHub.MarketParticipant.Application.Options;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Integration;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Monitor;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Options;
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

        services.AddOptions<SendGridOptions>().BindConfiguration(SendGridOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<ServiceBusOptions>().BindConfiguration(ServiceBusOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<CvrUpdateOptions>().BindConfiguration(CvrUpdateOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<BalanceResponsibleChangedOptions>().BindConfiguration(BalanceResponsibleChangedOptions.SectionName).ValidateDataAnnotations();

        services.AddScoped<SynchronizeActorsTimerTrigger>();
        services.AddScoped<EmailEventTimerTrigger>();
        services.AddScoped<UserInvitationExpiredTimerTrigger>();
        services.AddScoped<DispatchIntegrationEventsTrigger>();
        services.AddScoped<ReceiveIntegrationEventsTrigger>();

        services.AddScoped<SynchronizeActorsTimerTrigger>();
        services.AddScoped<EmailEventTimerTrigger>();
        services.AddScoped<UserInvitationExpiredTimerTrigger>();
        services.AddScoped<DispatchIntegrationEventsTrigger>();
        services.AddScoped<ReceiveIntegrationEventsTrigger>();
        services.AddScoped<OrganizationIdentityUpdateTrigger>();

        services.AddPublisher<IntegrationEventProvider>();
        services.Configure<PublisherOptions>(options =>
        {
            options.ServiceBusConnectionString = configuration.GetValue($"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.ProducerConnectionString)}", defaultValue: string.Empty)!;
            options.TopicName = configuration.GetValue($"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.SharedIntegrationEventTopic)}", defaultValue: string.Empty)!;
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

        AddHealthChecks(services);

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services)
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

        services.AddScoped<HealthCheckEndpoint>();

        services
            .AddHealthChecks()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddDbContextCheck<MarketParticipantDbContext>(customTestQuery: CheckExpiredEventsAsync, name: "expired_events", tags: [HealthChecksConstants.StatusHealthCheckTag])
            .AddDbContextCheck<MarketParticipantDbContext>(customTestQuery: CheckExpiredEmailsAsync, name: "expired_emails", tags: [HealthChecksConstants.StatusHealthCheckTag])
            .AddAzureServiceBusSubscription(
                provider => provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.HealthConnectionString,
                provider => provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.SharedIntegrationEventTopic,
                provider => provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.IntegrationEventSubscription)
            .AddServiceBusTopicSubscriptionDeadLetter(
                provider => provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.FullyQualifiedNamespace,
                provider => provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.SharedIntegrationEventTopic,
                provider => provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.IntegrationEventSubscription,
                _ => new DefaultAzureCredential(),
                "Dead letter integration events",
                [HealthChecksConstants.StatusHealthCheckTag])
            .AddSendGrid()
            .AddCheck<ActiveDirectoryB2BRolesHealthCheck>("AD B2B Roles Check");
    }
}
