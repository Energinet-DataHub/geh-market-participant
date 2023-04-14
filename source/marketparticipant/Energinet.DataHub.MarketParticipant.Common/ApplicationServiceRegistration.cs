﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.User;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using FluentValidation;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.Common
{
    internal static class ApplicationServiceRegistration
    {
        public static void AddApplicationServices(this Container container)
        {
            container.Register<IValidator<GetOrganizationsCommand>, GetOrganizationsCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetPermissionsCommand>, GetPermissionsCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetPermissionCommand>, GetPermissionCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<CreateOrganizationCommand>, CreateOrganizationCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<CreateActorCommand>, CreateActorCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<UpdateOrganizationCommand>, UpdateOrganizationCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<UpdateActorCommand>, UpdateActorCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetSingleOrganizationCommand>, GetSingleOrganizationCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetSingleActorCommand>, GetSingleActorCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetActorsCommand>, GetActorsCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetAllActorsCommand>, GetAllActorsCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetActorContactsCommand>, GetActorContactsCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<CreateActorContactCommand>, CreateActorContactCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<DeleteActorContactCommand>, DeleteActorContactCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<CreateGridAreaCommand>, CreateGridAreaCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<UpdateGridAreaCommand>, UpdateGridAreaCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetGridAreasCommand>, GetGridAreasCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetGridAreaCommand>, GetGridAreaCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<GetGridAreaOverviewCommand>, GetGridAreaOverviewCommandRuleSet>();
            container.Register<IValidator<GetGridAreaAuditLogEntriesCommand>, GetGridAreaAuditLogEntriesCommandRuleSet>();
            container.Register<IValidator<GetUserOverviewCommand>, GetUserOverviewCommandRuleSet>();
            container.Register<IValidator<GetUserCommand>, GetUserCommandRuleSet>();
            container.Register<IValidator<GetUserRolesCommand>, GetUserRolesCommandRuleSet>();
            container.Register<IValidator<GetUserAuditLogsCommand>, GetUserAuditLogEntriesCommandRuleSet>();
            container.Register<IValidator<GetUserRoleAuditLogsCommand>, GetUserRoleAuditLogEntriesCommandRuleSet>();
            container.Register<IValidator<GetUserPermissionsCommand>, GetUserPermissionsCommandRuleSet>();
            container.Register<IValidator<UpdatePermissionCommand>, UpdatePermissionCommandRuleSet>();
            container.Register<IValidator<GetAssociatedUserActorsCommand>, GetAssociatedUserActorsCommandRuleSet>();
            container.Register<IValidator<GetAllUserRolesCommand>, GetAllUserRolesCommandRuleSet>();
            container.Register<IValidator<GetAvailableUserRolesForActorCommand>, GetAvailableUserRolesForActorCommandRuleSet>();
            container.Register<IValidator<UpdateUserRoleAssignmentsCommand>, UpdateUserRoleAssignmentsCommandRuleSet>();
            container.Register<IValidator<GetUserRoleCommand>, GetUserRoleCommandRuleSet>();
            container.Register<IValidator<CreateUserRoleCommand>, CreateUserRoleCommandRuleSet>();
            container.Register<IValidator<UpdateUserRoleCommand>, UpdateUserRoleCommandRuleSet>();
            container.Register<IValidator<GetSelectionActorsQueryCommand>, GetSelectionActorsQueryCommandRuleSet>();
            container.Register<IValidator<SynchronizeActorsCommand>, SynchronizeActorsCommandRuleSet>();
            container.Register<IValidator<GetPermissionDetailsCommand>, GetPermissionDetailsCommandRuleSet>();
            container.Register<IValidator<InviteUserCommand>, InviteUserCommandRuleSet>();
            container.Register<IValidator<SendUserInviteEmailCommand>, SendUserInviteEmailCommandRuleSet>();
            container.Register<IValidator<GetPermissionAuditLogsCommand>, GetPermissionAuditLogEntriesCommandRuleSet>();
            container.Register<IValidator<GetUserRolesToPermissionCommand>, GetUserRolesToPermissionCommandRuleSet>();
            container.Register<IValidator<DeactivateUserRoleCommand>, DeactivateUserRoleCommandRuleSet>();

            container.Register<IActiveDirectoryB2CService, ActiveDirectoryB2CService>(Lifestyle.Scoped);
            container.Register<IOrganizationExistsHelperService, OrganizationExistsHelperService>(Lifestyle.Scoped);
            container.Register<IUserRoleAuditLogService, UserRoleAuditLogService>(Lifestyle.Scoped);
            container.Register<IExternalActorSynchronizationRepository, ExternalActorSynchronizationRepository>(Lifestyle.Scoped);
        }
    }
}