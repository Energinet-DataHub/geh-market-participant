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
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Builder;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
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
using Microsoft.Extensions.Logging;
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
        services.AddOptions<IntegrationEventsOptions>().BindConfiguration(IntegrationEventsOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<ServiceBusNamespaceOptions>().BindConfiguration(ServiceBusNamespaceOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<CvrUpdateOptions>().BindConfiguration(CvrUpdateOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<BalanceResponsibleChangedOptions>().BindConfiguration(BalanceResponsibleChangedOptions.SectionName).ValidateDataAnnotations();
        services.AddOptions<KeyVaultOptions>().BindConfiguration(KeyVaultOptions.SectionName).ValidateDataAnnotations();

        services.AddScoped<SynchronizeActorsTimerTrigger>();
        services.AddScoped<EmailEventTimerTrigger>();
        services.AddScoped<ReceiveIntegrationEventsTrigger>();
        services.AddScoped<UserInvitationExpiredTimerTrigger>();
        services.AddScoped<DispatchIntegrationEventsTrigger>();
        services.AddScoped<OrganizationIdentityUpdateTrigger>();

        services.AddServiceBusClientForApplication(configuration);
        services.AddIntegrationEventsPublisher<IntegrationEventProvider>(configuration);

        services.AddSubscriber<IntegrationEventSubscriptionHandler>(new[]
        {
            BalanceResponsiblePartiesChanged.Descriptor,
        });

        services.AddSendGrid((provider, options) =>
        {
            var sendGridOptions = provider.GetRequiredService<IOptions<SendGridOptions>>();
            options.ApiKey = sendGridOptions.Value.ApiKey;
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>();
            var defaultCredentials = new DefaultAzureCredential();
            return new SecretClient(options.Value.CertificatesKeyVault, defaultCredentials);
        });

        services.AddSingleton<ICertificateService>(s =>
        {
            var certificateClient = s.GetRequiredService<SecretClient>();
            var logger = s.GetRequiredService<ILogger<CertificateService>>();
            var certificateValidation = s.GetRequiredService<ICertificateValidation>();
            return new CertificateService(certificateClient, certificateValidation, logger);
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

        var defaultAzureCredential = new DefaultAzureCredential();
        services
            .AddHealthChecks()
            .AddDbContextCheck<MarketParticipantDbContext>()
            .AddDbContextCheck<MarketParticipantDbContext>(customTestQuery: CheckExpiredEventsAsync, name: "expired_events", tags: [HealthChecksConstants.StatusHealthCheckTag])
            .AddDbContextCheck<MarketParticipantDbContext>(customTestQuery: CheckExpiredEmailsAsync, name: "expired_emails", tags: [HealthChecksConstants.StatusHealthCheckTag])
            .AddAzureServiceBusSubscription(
                provider => provider.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                provider => provider.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value.TopicName,
                provider => provider.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value.SubscriptionName,
                _ => defaultAzureCredential)
            .AddServiceBusTopicSubscriptionDeadLetter(
                provider => provider.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                provider => provider.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value.TopicName,
                provider => provider.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value.SubscriptionName,
                _ => defaultAzureCredential,
                "Dead letter integration events",
                [HealthChecksConstants.StatusHealthCheckTag])
            .AddSendGrid()
            .AddCheck<ActiveDirectoryB2BRolesHealthCheck>("AD B2B Roles Check");
    }
}
