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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class User
{
    private const int UserInvitationExpiresAtHours = 24 * 7;
    private readonly SharedUserReferenceId? _sharedId;

    public User(ActorId administratedBy, SharedUserReferenceId sharedId, ExternalUserId externalId)
    {
        _sharedId = sharedId;
        Id = new UserId(Guid.Empty);
        AdministratedBy = administratedBy;
        ExternalId = externalId;
        RoleAssignments = new HashSet<UserRoleAssignment>();
    }

    public User(
        UserId id,
        ActorId administratedBy,
        ExternalUserId externalId,
        IEnumerable<UserRoleAssignment> roleAssignments,
        DateTimeOffset? mitIdSignupInitiatedAt,
        DateTimeOffset? invitationExpiresAt,
        DateTimeOffset? latestLoginAt)
    {
        _sharedId = null;
        Id = id;
        AdministratedBy = administratedBy;
        ExternalId = externalId;
        RoleAssignments = roleAssignments.ToHashSet();
        MitIdSignupInitiatedAt = mitIdSignupInitiatedAt;
        InvitationExpiresAt = invitationExpiresAt;
        LatestLoginAt = latestLoginAt;
    }

    public UserId Id { get; }
    public ActorId AdministratedBy { get; }
    public ExternalUserId ExternalId { get; }
    public SharedUserReferenceId SharedId => _sharedId ?? throw new InvalidOperationException("The shared reference id is only available when creating the entity.");
    public ICollection<UserRoleAssignment> RoleAssignments { get; }

    public DateTimeOffset? MitIdSignupInitiatedAt { get; private set; }
    public DateTimeOffset? InvitationExpiresAt { get; private set; }
    public DateTimeOffset? LatestLoginAt { get; }

    public bool ValidLogonRequirements => !InvitationExpiresAt.HasValue || InvitationExpiresAt >= DateTimeOffset.UtcNow;

    public void InitiateMitIdSignup()
    {
        MitIdSignupInitiatedAt = DateTimeOffset.UtcNow;
    }

    public void ActivateUserExpiration()
    {
        InvitationExpiresAt = DateTimeOffset.UtcNow.AddHours(UserInvitationExpiresAtHours);
    }

    public void DeactivateUserExpiration()
    {
        InvitationExpiresAt = null;
    }
}
