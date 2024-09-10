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

    public const string DelegationsForActorViewed = "DelegationsForActorViewed";
    public const string ActorDelegationStarted = "ActorDelegationStarted";
    public const string ActorDelegationStopped = "ActorDelegationStopped";

    public const string UserRoleEdited = "UserRoleEdited";
}
