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

using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.Common;

internal static class RepositoryRegistration
{
    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IActorContactRepository, ActorContactRepository>();
        services.AddScoped<IGridAreaRepository, GridAreaRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IGridAreaLinkRepository, GridAreaLinkRepository>();
        services.AddScoped<IMarketRoleAndGridAreaForActorReservationService, MarketRoleAndGridAreaForActorReservationService>();
        services.AddScoped<IGridAreaOverviewRepository, GridAreaOverviewRepository>();
        services.AddScoped<IUserOverviewRepository, UserOverviewRepository>();
        services.AddScoped<IGridAreaAuditLogRepository, GridAreaAuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IActorRepository, ActorRepository>();
        services.AddScoped<IUserQueryRepository, UserQueryRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IUserRoleAssignmentAuditLogRepository, UserRoleAssignmentAuditLogRepository>();
        services.AddScoped<IUserRoleAuditLogRepository, UserRoleAuditLogRepository>();
        services.AddScoped<IUserInviteAuditLogRepository, UserInviteAuditLogRepository>();
        services.AddScoped<IUserIdentityAuditLogRepository, UserIdentityAuditLogRepository>();
        services.AddScoped<IPermissionAuditLogRepository, PermissionAuditLogRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IDomainEventRepository, DomainEventRepository>();
        services.AddScoped<IEmailEventRepository, EmailEventRepository>();
        services.AddScoped<IOrganizationAuditLogRepository, OrganizationAuditLogRepository>();
        services.AddScoped<IActorAuditLogRepository, ActorAuditLogRepository>();
        services.AddScoped<IActorContactAuditLogRepository, ActorContactAuditLogRepository>();
        services.AddScoped<IProcessDelegationAuditLogRepository, ProcessDelegationAuditLogRepository>();
        services.AddScoped<IOrganizationIdentityRepository, OrganizationIdentityRepository>();
        services.AddScoped<IProcessDelegationRepository, ProcessDelegationRepository>();
        services.AddScoped<IBalanceResponsibilityRequestRepository, BalanceResponsibilityRequestRepository>();
        services.AddScoped<IBalanceResponsibilityRelationsRepository, BalanceResponsibilityRelationsRepository>();
        services.AddScoped<IActorConsolidationRepository, ActorConsolidationRepository>();
        services.AddScoped<IActorConsolidationAuditLogRepository, ActorConsolidationAuditLogRepository>();
        services.AddScoped<IDownloadTokenRespository, DownloadTokenRespository>();
        services.AddScoped<IB2CLogRepository, B2CLogRepository>();
        services.AddScoped<ICutoffRepository, CutoffRepository>();
        services.AddScoped<IAdditionalRecipientRepository, AdditionalRecipientRepository>();
        services.AddScoped<IAdditionalRecipientQueryRepository, AdditionalRecipientQueryRepository>();
    }
}
