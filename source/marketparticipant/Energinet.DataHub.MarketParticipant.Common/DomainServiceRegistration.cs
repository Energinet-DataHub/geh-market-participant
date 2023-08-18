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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.Common;

internal static class DomainServiceRegistration
{
    public static void AddDomainServices(this Container container)
    {
        container.Register<IUniqueGlobalLocationNumberRuleService, UniqueGlobalLocationNumberRuleService>(Lifestyle.Scoped);
        container.Register<IOverlappingEicFunctionsRuleService, OverlappingEicFunctionsRuleService>(Lifestyle.Scoped);
        container.Register<IOverlappingActorContactCategoriesRuleService, OverlappingActorContactCategoriesRuleService>(Lifestyle.Scoped);

        container.Register<IExternalActorIdConfigurationService, ExternalActorIdConfigurationService>(Lifestyle.Scoped);
        container.Register<IUniqueMarketRoleGridAreaRuleService, UniqueMarketRoleGridAreaRuleService>(Lifestyle.Scoped);
        container.Register<IUniqueOrganizationBusinessRegisterIdentifierService, UniqueOrganizationBusinessRegisterIdentifierService>(Lifestyle.Scoped);
        container.Register<IEnsureUserRolePermissionsService, EnsureUserRolePermissionsService>();

        container.Register<IActorFactoryService, ActorFactoryService>(Lifestyle.Scoped);
        container.Register<IOrganizationFactoryService, OrganizationFactoryService>(Lifestyle.Scoped);
        container.Register<IGridAreaFactoryService, GridAreaFactoryService>(Lifestyle.Scoped);

        container.Register<IUserInvitationService, UserInvitationService>(Lifestyle.Scoped);
        container.Register<IOrganizationDomainValidationService, OrganizationDomainValidationService>(Lifestyle.Scoped);
        container.Register<IUserStatusCalculator, UserStatusCalculator>(Lifestyle.Scoped);

        container.Register<IPasswordChecker, PasswordChecker>(Lifestyle.Scoped);
        container.Register<IPasswordGenerator, PasswordGenerator>(Lifestyle.Scoped);
        container.Register<IUserPasswordGenerator, UserPasswordGenerator>(Lifestyle.Scoped);
    }
}
