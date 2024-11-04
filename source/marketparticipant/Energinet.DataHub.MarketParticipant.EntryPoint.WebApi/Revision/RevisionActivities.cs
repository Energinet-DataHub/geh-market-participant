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

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;

public static class RevisionActivities
{
    public const string PublicActorContactsRetrieved = "PublicActorContactsRetrieved";
    public const string AllContactsForActorRetrieved = "AllContactsForActorRetrieved";
    public const string CreateContactForActor = "CreateContactForActor";
    public const string DeleteContactForActor = "DeleteContactForActor";

    public const string AllActorsRetrieved = "AllActorsRetrieved";
    public const string ActorRetrieved = "ActorRetrieved";
    public const string ActorCreated = "ActorCreated";
    public const string ActorEdited = "ActorEdited";
    public const string ActorAuditLogViewed = "ActorAuditLogViewed";

    public const string ActorCredentialsViewed = "ActorCredentialsViewed";
    public const string ActorCredentialsRemoved = "ActorCredentialsRemoved";
    public const string ActorCertificateAssigned = "ActorCertificateAssigned";
    public const string ActorClientSecretAssigned = "ActorClientSecretAssigned";

    public const string BalanceResponsibilityRelationsForActorViewed = "BalanceResponsibilityRelationsForActorViewed";
    public const string BalanceResponsibilityRelationsImported = "BalanceResponsibilityRelationsImported";
    public const string DelegationsForActorViewed = "DelegationsForActorViewed";
    public const string ActorDelegationStarted = "ActorDelegationStarted";
    public const string ActorDelegationStopped = "ActorDelegationStopped";

    public const string AuditIdentityLookup = "AuditIdentityLookup";

    public const string UserInvited = "UserInvited";
    public const string UserReInvited = "UserReInvited";
    public const string UserDomainLookup = "UserDomainLookup";

    public const string UserRetrieved = "UserRetrieved";
    public const string UsersRetrieved = "UsersRetrieved";
    public const string UserActorsRetrieved = "UserActorsRetrieved";
    public const string UserEdited = "UserEdited";
    public const string UserDeactivated = "UserDeactivated";
    public const string UserReactivated = "UserReactivated";
    public const string UserResetMfa = "UserResetMfa";
    public const string UserProfileEdited = "UserProfileEdited";
    public const string UserProfileFederate = "UserProfileFederate";
    public const string UserProfileRemoveFederation = "UserProfileRemoveFederation";
    public const string UserAuditLogViewed = "UserAuditLogViewed";

    public const string PublicGridAreasRetrieved = "PublicGridAreasRetrieved";
    public const string GridAreaCreated = "GridAreaCreated";
    public const string GridAreaEdited = "GridAreaEdited";
    public const string GridAreaAuditLogViewed = "GridAreaAuditLogViewed";

    public const string AllOrganizationsRetrieved = "AllOrganizationsRetrieved";
    public const string OrganizationActorsRetrieved = "OrganizationActorsRetrieved";
    public const string OrganizationRetrieved = "OrganizationRetrieved";
    public const string OrganizationCreated = "OrganizationCreated";
    public const string OrganizationEdited = "OrganizationEdited";
    public const string OrganizationAuditLogViewed = "OrganizationAuditLogViewed";

    public const string AllPermissionsViewed = "AllPermissionsViewed";
    public const string PermissionOverview = "PermissionOverview";
    public const string PermissionViewed = "PermissionViewed";
    public const string PermissionEdited = "PermissionEdited";
    public const string PermissionAuditLogViewed = "PermissionAuditLogViewed";
    public const string PermissionDetailsViewed = "PermissionDetailsViewed";

    public const string PossibleUserRoleAssignmentsViewed = "PossibleUserRoleAssignmentsViewed";
    public const string UserRoleAssignmentsViewed = "UserRoleAssignmentsViewed";
    public const string UserRolesAssigned = "UserRolesAssigned";

    public const string AllUserRolesRetrieved = "AllUserRolesRetrieved";
    public const string UserRoleRetrieved = "UserRoleRetrieved";
    public const string UserRoleCreated = "UserRoleCreated";
    public const string UserRoleEdited = "UserRoleEdited";
    public const string UserRoleAuditLogViewed = "UserRoleAuditLogViewed";
    public const string UserRoleDeactivated = "UserRoleDeactivated";
    public const string UserRolesAssignedToPermission = "UserRolesAssignedToPermission";
}
