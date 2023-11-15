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

using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Common;

internal static class InfrastructureRegistration
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserIdentityAuthenticationService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var enforce2Fa = configuration.GetOptionalSetting(Settings.Enforce2Fa);

            var graphServiceClient = sp.GetRequiredService<GraphServiceClient>();
            return new UserIdentityAuthenticationService(graphServiceClient, enforce2Fa);
        });

        services.AddScoped<IIntegrationEventFactory<ActorActivated>, ActorActivatedIntegrationEventFactory>();
        services.AddScoped<IIntegrationEventFactory<ActorCertificateCredentialsAssigned>, ActorCertificateCredentialsAssignedIntegrationEventFactory>();
        services.AddScoped<IIntegrationEventFactory<GridAreaOwnershipAssigned>, GridAreaOwnershipAssignedIntegrationEventFactory>();
    }
}
