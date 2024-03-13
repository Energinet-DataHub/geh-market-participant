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

using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.Common;

internal static class DomainServiceRegistration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUniqueGlobalLocationNumberRuleService, UniqueGlobalLocationNumberRuleService>();
        services.AddScoped<IOverlappingEicFunctionsRuleService, OverlappingEicFunctionsRuleService>();
        services.AddScoped<IOverlappingActorContactCategoriesRuleService, OverlappingActorContactCategoriesRuleService>();

        services.AddScoped<IExternalActorIdConfigurationService, ExternalActorIdConfigurationService>();
        services.AddScoped<IUniqueMarketRoleGridAreaRuleService, UniqueMarketRoleGridAreaRuleService>();
        services.AddScoped<IUniqueOrganizationBusinessRegisterIdentifierService, UniqueOrganizationBusinessRegisterIdentifierService>();
        services.AddScoped<IEnsureUserRolePermissionsService, EnsureUserRolePermissionsService>();

        services.AddScoped<IActorFactoryService, ActorFactoryService>();
        services.AddScoped<IOrganizationFactoryService, OrganizationFactoryService>();
        services.AddScoped<IGridAreaFactoryService, GridAreaFactoryService>();

        services.AddScoped<IUserInvitationService, UserInvitationService>();
        services.AddScoped<IOrganizationDomainValidationService, OrganizationDomainValidationService>();
        services.AddScoped<IUserStatusCalculator, UserStatusCalculator>();

        services.AddScoped<IPasswordChecker, PasswordChecker>();
        services.AddScoped<IPasswordGenerator, PasswordGenerator>();
        services.AddScoped<IUserPasswordGenerator, UserPasswordGenerator>();

        services.AddScoped<IEntityLock, EntityLock>();
    }
}
