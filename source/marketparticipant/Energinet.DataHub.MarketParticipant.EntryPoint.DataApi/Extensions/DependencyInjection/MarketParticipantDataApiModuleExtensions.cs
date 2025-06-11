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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.DataApi.Extensions.DependencyInjection;

internal static class MarketParticipantDataApiModuleExtensions
{
    public static IServiceCollection AddMarketParticipantDataApiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarketParticipantCore();

        services.AddScoped<IAuditIdentityProvider>(_ => new ForbiddenAuditIdentityProvider());
        services.AddScoped<IUserContext<FrontendUser>>(_ => new ForbiddenUser());
        services.AddScoped<ICertificateService>(_ => throw new InvalidOperationException("The current host is not configured to use certificates."));
        services.AddFeatureManagement();

        return services;
    }
}
