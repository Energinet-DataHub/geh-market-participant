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
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.Common
{
    internal static class ApplicationServiceRegistration
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IValidator<GetOrganizationsCommand>, GetOrganizationsCommandRuleSet>();
            services.AddScoped<IValidator<GetPermissionsCommand>, GetPermissionsCommandRuleSet>();
            services.AddScoped<IValidator<GetPermissionCommand>, GetPermissionCommandRuleSet>();
            services.AddScoped<IValidator<CreateOrganizationCommand>, CreateOrganizationCommandRuleSet>();
            services.AddScoped<IValidator<CreateActorCommand>, CreateActorCommandRuleSet>();
            services.AddScoped<IValidator<UpdateOrganizationCommand>, UpdateOrganizationCommandRuleSet>();
            services.AddScoped<IValidator<UpdateActorCommand>, UpdateActorCommandRuleSet>();
            services.AddScoped<IValidator<GetSingleOrganizationCommand>, GetSingleOrganizationCommandRuleSet>();
            services.AddScoped<IValidator<GetSingleActorCommand>, GetSingleActorCommandRuleSet>();
            services.AddScoped<IValidator<GetActorsCommand>, GetActorsCommandRuleSet>();
            services.AddScoped<IValidator<GetAllActorsCommand>, GetAllActorsCommandRuleSet>();
            services.AddScoped<IValidator<GetActorContactsCommand>, GetActorContactsCommandRuleSet>();
            services.AddScoped<IValidator<CreateActorContactCommand>, CreateActorContactCommandRuleSet>();
            services.AddScoped<IValidator<DeleteActorContactCommand>, DeleteActorContactCommandRuleSet>();
            services.AddScoped<IValidator<CreateGridAreaCommand>, CreateGridAreaCommandRuleSet>();
            services.AddScoped<IValidator<UpdateGridAreaCommand>, UpdateGridAreaCommandRuleSet>();
            services.AddScoped<IValidator<GetGridAreasCommand>, GetGridAreasCommandRuleSet>();
            services.AddScoped<IValidator<GetGridAreaCommand>, GetGridAreaCommandRuleSet>();
            services.AddScoped<IValidator<GetGridAreaOverviewCommand>, GetGridAreaOverviewCommandRuleSet>();
            services.AddScoped<IValidator<GetGridAreaAuditLogEntriesCommand>, GetGridAreaAuditLogEntriesCommandRuleSet>();
            services.AddScoped<IValidator<GetUserOverviewCommand>, GetUserOverviewCommandRuleSet>();
            services.AddScoped<IValidator<GetUserCommand>, GetUserCommandRuleSet>();
            services.AddScoped<IValidator<GetUserRolesCommand>, GetUserRolesCommandRuleSet>();
            services.AddScoped<IValidator<GetUserAuditLogsCommand>, GetUserAuditLogEntriesCommandRuleSet>();
            services.AddScoped<IValidator<GetUserRoleAuditLogsCommand>, GetUserRoleAuditLogEntriesCommandRuleSet>();
            services.AddScoped<IValidator<GetUserPermissionsCommand>, GetUserPermissionsCommandRuleSet>();
            services.AddScoped<IValidator<UpdatePermissionCommand>, UpdatePermissionCommandRuleSet>();
            services.AddScoped<IValidator<GetActorsAssociatedWithUserCommand>, GetActorsAssociatedWithUserCommandRuleSet>();
            services.AddScoped<IValidator<GetActorsAssociatedWithExternalUserIdCommand>, GetActorsAssociatedWithExternalUserIdCommandRuleSet>();
            services.AddScoped<IValidator<GetAllUserRolesCommand>, GetAllUserRolesCommandRuleSet>();
            services.AddScoped<IValidator<GetAvailableUserRolesForActorCommand>, GetAvailableUserRolesForActorCommandRuleSet>();
            services.AddScoped<IValidator<UpdateUserRoleAssignmentsCommand>, UpdateUserRoleAssignmentsCommandRuleSet>();
            services.AddScoped<IValidator<GetUserRoleCommand>, GetUserRoleCommandRuleSet>();
            services.AddScoped<IValidator<CreateUserRoleCommand>, CreateUserRoleCommandRuleSet>();
            services.AddScoped<IValidator<UpdateUserRoleCommand>, UpdateUserRoleCommandRuleSet>();
            services.AddScoped<IValidator<GetSelectionActorsQueryCommand>, GetSelectionActorsQueryCommandRuleSet>();
            services.AddScoped<IValidator<SynchronizeActorsCommand>, SynchronizeActorsCommandRuleSet>();
            services.AddScoped<IValidator<GetPermissionDetailsCommand>, GetPermissionDetailsCommandRuleSet>();
            services.AddScoped<IValidator<InviteUserCommand>, InviteUserCommandRuleSet>();
            services.AddScoped<IValidator<ReInviteUserCommand>, ReInviteUserCommandRuleSet>();
            services.AddScoped<IValidator<SendUserInviteEmailCommand>, SendUserInviteEmailCommandRuleSet>();
            services.AddScoped<IValidator<GetPermissionAuditLogsCommand>, GetPermissionAuditLogEntriesCommandRuleSet>();
            services.AddScoped<IValidator<GetUserRolesToPermissionCommand>, GetUserRolesToPermissionCommandRuleSet>();
            services.AddScoped<IValidator<DeactivateUserRoleCommand>, DeactivateUserRoleCommandRuleSet>();
            services.AddScoped<IValidator<UpdateUserIdentityCommand>, UpdateUserIdentityCommandRuleSet>();
            services.AddScoped<IValidator<InitiateMitIdSignupCommand>, InitiateMitIdSignupCommandRuleSet>();
            services.AddScoped<IValidator<DeactivateUserCommand>, DeactivateUserCommandRuleSet>();
            services.AddScoped<IValidator<UserInvitationExpiredCommand>, UserInvitationExpiredCommandRuleSet>();
            services.AddScoped<IValidator<GetAuditIdentityCommand>, GetAuditIdentityCommandRuleSet>();
            services.AddScoped<IValidator<GetOrganizationAuditLogsCommand>, GetOrganizationAuditLogEntriesCommandRuleSet>();
            services.AddScoped<IValidator<GetActorAuditLogsCommand>, GetActorAuditLogEntriesCommandRuleSet>();
            services.AddScoped<IValidator<ResetUserTwoFactorAuthenticationCommand>, ResetUserTwoFactorAuthenticationRuleSet>();
            services.AddScoped<IValidator<CheckEmailExistsCommand>, CheckEmailExistsCommandRuleSet>();
            services.AddScoped<IValidator<AssignActorCertificateCommand>, AssignActorCertificateRuleSet>();
            services.AddScoped<IValidator<RemoveActorCertificateCommand>, RemoveActorCertificateCommandRuleSet>();
            services.AddScoped<IValidator<GetActorCredentialsCommand>, GetActorCredentialsCommandRuleSet>();

            services.AddScoped<IActiveDirectoryB2CService, ActiveDirectoryB2CService>();
            services.AddScoped<IOrganizationExistsHelperService, OrganizationExistsHelperService>();
            services.AddScoped<IExternalActorSynchronizationRepository, ExternalActorSynchronizationRepository>();
            services.AddScoped<IUserIdentityOpenIdLinkService, UserIdentityOpenIdLinkService>();
        }
    }
}
